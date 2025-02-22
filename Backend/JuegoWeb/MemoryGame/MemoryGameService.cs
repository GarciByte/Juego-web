using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Dtos;
using JuegoWeb.Services;
using JuegoWeb.WebSocketAdvanced;
using System.Collections.Concurrent;

namespace JuegoWeb.MemoryGame;

public class MemoryGameService
{
    private readonly IServiceProvider _serviceProvider;

    // Diccionario con las partidas activas
    private readonly ConcurrentDictionary<long, MemoryGame> _activeGames = new();

    // Tiempo máximo de 1 minuto para cada movimiento del jugador
    private readonly TimeSpan TurnDuration = TimeSpan.FromMinutes(1);

    // Semáforo para el reinicio del temporizador
    private readonly SemaphoreSlim _timerSemaphore = new SemaphoreSlim(1, 1);

    public MemoryGameService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Obtiene la cantidad de partidas activas en curso
    public int GetActiveGameCount()
    {
        return _activeGames.Count;
    }

    // Obtiene la cantidad total de jugadores que están en partidas activas
    public int GetActivePlayersCount()
    {
        int totalPlayers = 0;
        foreach (var game in _activeGames.Values)
        {
            foreach (var player in game.Players)
            {
                if (!player.IsBot)
                {
                    totalPlayers++;
                }
            }
        }
        return totalPlayers;
    }

    // Reinicia el temporizador del turno
    private async Task RestartTurnTimerAsync(MemoryGame game)
    {
        await _timerSemaphore.WaitAsync();
        try
        {
            if (game.TurnCancellationTokenSource != null)
            {
                game.TurnCancellationTokenSource.Cancel();
                game.TurnCancellationTokenSource.Dispose();
            }

            game.TurnCancellationTokenSource = new CancellationTokenSource();
        }
        finally
        {
            _timerSemaphore.Release();
        }

        try
        {
            await Task.Delay(TurnDuration, game.TurnCancellationTokenSource.Token);

            // Cuando se agota el tiempo del turno, el jugador pierde la partida
            await HandleTurnTimeoutAsync(game);
        }
        catch (TaskCanceledException)
        {
        }
    }

    // Envía un mensaje a un usuario en específico
    public async Task SendWebSocketMessageAsync(int userId, WebSocketMessage message)
    {
        using var scope = _serviceProvider.CreateScope();
        var webSocketMessageSender = scope.ServiceProvider.GetRequiredService<IWebSocketMessageSender>();
        await webSocketMessageSender.SendToUserAsync(userId, message);
    }

    // Guarda el resultado de una partida
    public async Task<GameHistoryDto> SaveHistorialAsync(GameHistory history)
    {
        using var scope = _serviceProvider.CreateScope();
        var gameHistoryService = scope.ServiceProvider.GetRequiredService<GameHistoryService>();
        var gameHistory = await gameHistoryService.AddGameHistoryAsync(history);

        return gameHistory;
    }

    // Crea una nueva partida
    public void CreateGame(GameRoomDto room)
    {
        if (_activeGames.TryGetValue(room.RoomId, out var existingGame))
        {
            Console.WriteLine($"Ya existe una partida con ID: {existingGame.RoomId}");
        }
        else
        {
            var game = new MemoryGame
            {
                RoomId = room.RoomId,
                StartTime = DateTime.UtcNow,
                CurrentPlayerIndex = 0
            };

            // Agrega al primer jugador
            game.Players.Add(new MemoryPlayer { UserId = room.HostUserId, Score = 0 });

            // Agrega al segundo jugador
            if (room.RoomType == GameRoomType.Bot)
            {
                game.Players.Add(new MemoryPlayer { UserId = -1, Score = 0, IsBot = true }); // Bot
            }
            else if (room.GuestUserId.HasValue)
            {
                game.Players.Add(new MemoryPlayer { UserId = room.GuestUserId.Value, Score = 0 });
            }

            // Se crea un tablero de 16 cartas (8 parejas)
            game.Cards = GenerateBoard(8);

            // Guarda la partida en el diccionario
            _activeGames[game.RoomId] = game;

            Console.WriteLine($"Ha empezado una nueva partida con los usuarios con ID: {game.Players[0].UserId} y {game.Players[1].UserId}.");

            // Inicia el temporizador
            _ = RestartTurnTimerAsync(game);

            // Envía un mensaje de inicio de partida a cada jugador
            foreach (var player in game.Players.Where(p => !p.IsBot))
            {
                _ = SendWebSocketMessageAsync(player.UserId, new WebSocketMessage
                {
                    Type = MsgType.GameStart,
                    Id = player.UserId,
                    Content = new
                    {
                        GameRoomId = game.RoomId,
                        CurrentTurnUserId = game.Players[game.CurrentPlayerIndex].UserId,
                        Board = GetBoardForPlayer(game)
                    }
                });
            }
        }
    }

    // Genera el tablero del juego
    private List<MemoryCard> GenerateBoard(int pairs)
    {
        // Almacena los valores de las cartas
        List<int> cardValues = [];

        for (int i = 1; i <= pairs; i++)
        {
            cardValues.Add(i);
            cardValues.Add(i);
        }

        // Se mezcla la lista de valores
        Random random = new();
        cardValues = cardValues.OrderBy(x => random.Next()).ToList();

        // Crea la lista de MemoryCard
        List<MemoryCard> boardCards = [];

        for (int i = 0; i < cardValues.Count; i++)
        {
            MemoryCard card = new()
            {
                CardId = i,
                Value = cardValues[i],
                IsFaceUp = false,
                IsMatched = false
            };
            boardCards.Add(card);
        }

        return boardCards;
    }

    // Información del tablero que se envía a los usuarios
    private object GetBoardForPlayer(MemoryGame game)
    {
        // Crea una lista de cartas
        var board = game.Cards.Select(c =>
        {
            // Valor inicial de la carta que se muestra 
            int? valueToShow = null;

            // Si la carta está volteada o ya fue emparejada
            if (c.IsFaceUp || c.IsMatched)
            {
                valueToShow = c.Value;
            }

            return new
            {
                c.CardId,
                Value = valueToShow,
                c.IsMatched
            };

        }).ToList();

        return board;
    }

    // Procesa el movimiento de un jugador
    public async Task ProcessMoveAsync(long roomId, int userId, MemoryGameMoveDto move)
    {
        // Comprueba que exista la partida
        if (_activeGames.TryGetValue(roomId, out var game))
        {
            var currentPlayer = game.Players[game.CurrentPlayerIndex];

            // Comprueba que el turno pertenezca al jugador que hizo el movimiento
            if (currentPlayer.UserId == userId)
            {
                // Busca la carta seleccionada
                var card = game.Cards.FirstOrDefault(c => c.CardId == move.CardId);
                if (card != null && !card.IsFaceUp && !card.IsMatched)
                {
                    // Voltea la carta y la agrega a la lista de cartas volteadas
                    card.IsFaceUp = true;
                    game.CurrentFlippedCards.Add(card);

                    // Reinicia el temporizador del turno
                    _ = RestartTurnTimerAsync(game);

                    // Notifica la actualización del tablero
                    await BroadcastGameUpdateAsync(game);

                    // Si se han volteado dos cartas, se verifica si son pareja
                    if (game.CurrentFlippedCards.Count == 2)
                    {
                        var first = game.CurrentFlippedCards[0];
                        var second = game.CurrentFlippedCards[1];

                        // Si las cartas coinciden
                        if (first.Value == second.Value)
                        {
                            currentPlayer.Score++;
                            first.IsMatched = true;
                            second.IsMatched = true;
                            game.CurrentFlippedCards.Clear();

                            // Si se han emparejado todas las cartas, se finaliza la partida
                            if (game.Cards.All(c => c.IsMatched))
                            {
                                await EndGameAsync(game);
                            }
                        }

                        // Si las cartas no coinciden
                        else
                        {
                            await Task.Delay(2000);
                            first.IsFaceUp = false;
                            second.IsFaceUp = false;
                            game.CurrentFlippedCards.Clear();
                            ChangeTurn(game); // Cambia de turno
                        }
                        await BroadcastGameUpdateAsync(game);
                    }
                }
            }
        }
    }


    // Envía la actualización del juego a cada jugador
    private async Task BroadcastGameUpdateAsync(MemoryGame game)
    {
        foreach (var player in game.Players.Where(p => !p.IsBot))
        {
            await SendWebSocketMessageAsync(player.UserId, new WebSocketMessage
            {
                Type = MsgType.GameUpdate,
                Id = player.UserId,
                Content = new
                {
                    Board = GetBoardForPlayer(game),
                    CurrentTurnUserId = game.Players[game.CurrentPlayerIndex].UserId,
                    Scores = game.Players.Select(p => new { p.UserId, p.Score })
                }
            });
        }
    }

    // Cambia el turno al siguiente jugador
    private void ChangeTurn(MemoryGame game)
    {
        int nextPlayerIndex = game.CurrentPlayerIndex + 1;
        if (nextPlayerIndex >= game.Players.Count)
        {
            nextPlayerIndex = 0;
        }
        game.CurrentPlayerIndex = nextPlayerIndex;

        // Reinicia el temporizador del turno
        _ = RestartTurnTimerAsync(game);

        // Si el siguiente jugador es un bot, ejecuta su movimiento
        if (game.Players[game.CurrentPlayerIndex].IsBot)
        {
            _ = ProcessBotMoveAsync(game);
        }
    }

    // Procesa la jugada del bot de manera aleatoria
    private async Task ProcessBotMoveAsync(MemoryGame game)
    {
        var random = new Random();
        await Task.Delay(random.Next(1000, 2000));

        // Mientras sea turno del bot y queden cartas disponibles
        while (game.Players[game.CurrentPlayerIndex].IsBot && game.Cards.Any(c => !c.IsMatched))
        {
            // Voltea 2 cartas
            while (game.CurrentFlippedCards.Count < 2 && game.Cards.Any(c => !c.IsFaceUp && !c.IsMatched))
            {
                var availableCards = game.Cards.Where(c => !c.IsFaceUp && !c.IsMatched).ToList();
                var selectedCard = availableCards[random.Next(availableCards.Count)];

                // Voltear la carta seleccionada
                selectedCard.IsFaceUp = true;
                game.CurrentFlippedCards.Add(selectedCard);

                await BroadcastGameUpdateAsync(game);

                // Espera antes de voltear la segunda carta
                if (game.CurrentFlippedCards.Count < 2)
                {
                    await Task.Delay(random.Next(1000, 2000));
                }
            }

            // Procesar la jugada cuando se hayan volteado 2 cartas
            if (game.CurrentFlippedCards.Count == 2)
            {
                var firstCard = game.CurrentFlippedCards[0];
                var secondCard = game.CurrentFlippedCards[1];

                // Si las cartas coinciden
                if (firstCard.Value == secondCard.Value)
                {
                    game.Players[game.CurrentPlayerIndex].Score++;
                    firstCard.IsMatched = true;
                    secondCard.IsMatched = true;
                    game.CurrentFlippedCards.Clear();

                    await BroadcastGameUpdateAsync(game);

                    // Si se han emparejado todas las cartas, se finaliza la partida
                    if (game.Cards.All(c => c.IsMatched))
                    {
                        await EndGameAsync(game);
                        break;
                    }
                    // Si no, se reinicia el temporizador del turno
                    else
                    {
                        _ = RestartTurnTimerAsync(game);
                        await Task.Delay(random.Next(1000, 2000));
                    }
                }

                // Si las cartas no coinciden
                else
                {
                    await Task.Delay(2000);
                    firstCard.IsFaceUp = false;
                    secondCard.IsFaceUp = false;
                    game.CurrentFlippedCards.Clear();

                    _ = RestartTurnTimerAsync(game);
                    ChangeTurn(game); // Cambia de turno
                    await BroadcastGameUpdateAsync(game);
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }

    // Cuando a un jugador se le agota el tiempo del turno
    public async Task HandleTurnTimeoutAsync(MemoryGame game)
    {
        int timeoutPlayerId = game.Players[game.CurrentPlayerIndex].UserId;
        Console.WriteLine($"Al usuario con ID: {timeoutPlayerId} se le ha acabado el tiempo del turno.");
        await EndGameAsync(game, timeoutPlayerId);
    }

    // Finaliza la partida
    private async Task EndGameAsync(MemoryGame game, int timeoutPlayerId = 0)
    {
        if (game.TurnCancellationTokenSource != null)
        {
            game.TurnCancellationTokenSource.Cancel();
            game.TurnCancellationTokenSource.Dispose();
        }

        game.EndTime = DateTime.UtcNow;
        var gameHistoryDtoList = await SaveHistorialAsync(game, timeoutPlayerId);

        foreach (var history in gameHistoryDtoList)
        {
            await SendWebSocketMessageAsync(history.UserId, new WebSocketMessage
            {
                Type = MsgType.GameOver,
                Id = history.UserId,
                Content = new
                {
                    Result = history
                }
            });
        }

        game.GameFinished = true;
        Console.WriteLine($"La partida con ID: {game.RoomId} a finalizado.");
    }

    // Guarda en la base de datos el historial de la partida para cada jugador
    private async Task<List<GameHistoryDto>> SaveHistorialAsync(MemoryGame game, int timeoutPlayerId = 0)
    {
        string playersStr = string.Join(",", game.Players.Select(p => p.UserId.ToString()));
        var gameHistoryDtoList = new List<GameHistoryDto>();

        foreach (var player in game.Players.Where(p => !p.IsBot))
        {
            string result;

            // Si a un usuario se le acabó el tiempo o abandonó la partida
            if (timeoutPlayerId != 0)
            {
                if (player.UserId == timeoutPlayerId)
                {
                    result = "Perdedor";
                }
                else
                {
                    result = "Ganador";
                }
            }
            else
            {
                // Se calcula la puntuación máxima obtenida
                int maxScore = game.Players.Max(p => p.Score);
                var winners = game.Players.Where(p => p.Score == maxScore).ToList();

                // Si hay un único jugador con la punuación más alta
                if (winners.Count == 1)
                {
                    if (player.UserId == winners.First().UserId)
                    {
                        result = "Ganador";
                    }
                    else
                    {
                        result = "Perdedor";
                    }
                }
                else
                {
                    result = "Empate";
                }
            }

            // Calcula la duración de la partida
            TimeSpan timeSpan = game.EndTime.Value - game.StartTime;
            TimeSpan duration = new(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

            var history = new GameHistory
            {
                GameName = "Juego de Memoria",
                Score = player.Score,
                Players = playersStr,
                Result = result,
                Duration = duration,
                UserId = player.UserId
            };

            var gameHistoryDto = await SaveHistorialAsync(history);
            gameHistoryDtoList.Add(gameHistoryDto);
        }

        return gameHistoryDtoList;
    }

    // Solicitud de revancha
    public async Task RequestRematchAsync(GameRoomDto room, int userId)
    {
        if (_activeGames.TryGetValue(room.RoomId, out var game))
        {
            if (game.GameFinished)
            {
                if (room.RoomType == GameRoomType.Bot)
                {
                    _activeGames.TryRemove(game.RoomId, out _);
                    game.GameFinished = false;
                    game.Players.Clear();
                    game.Cards.Clear();
                    CreateGame(room);
                }
                else
                {
                    var firstPlayer = game.Players.FirstOrDefault(p => p.UserId == room.HostUserId);
                    var secondPlayer = game.Players.FirstOrDefault(p => p.UserId == room.GuestUserId);

                    if (firstPlayer != null && secondPlayer != null)
                    {
                        var currentplayer = game.Players.FirstOrDefault(p => p.UserId == userId);
                        currentplayer.RequestRematch = true;

                        if (game.Players.All(p => p.RequestRematch))
                        {
                            _activeGames.TryRemove(game.RoomId, out _);
                            game.GameFinished = false;
                            game.Players.Clear();
                            game.Cards.Clear();
                            CreateGame(room);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("La partida no había terminado.");
            }
        }
        else
        {
            await CancelRequestRematch(userId);
        }
    }

    // Eliminar una partida
    public async Task DeleteGameAsync(long roomId, int userId)
    {
        if (_activeGames.TryGetValue(roomId, out var game))
        {
            if (!game.GameFinished)
            {
                Console.WriteLine($"El usuario con ID {userId} ha abandonado la partida.");
                await EndGameAsync(game, userId);
            }
            else
            {
                var playerRequestRematch = game.Players.FirstOrDefault(p => p.RequestRematch);
                if (playerRequestRematch != null)
                {
                    await CancelRequestRematch(playerRequestRematch.UserId);
                }
            }
            _activeGames.TryRemove(game.RoomId, out _);
        }
        else
        {
            Console.WriteLine("El jugador no estaba en una partida en curso.");
        }
    }

    // Si un usuario quería una revancha y el otro no la aceptó.
    public async Task CancelRequestRematch(int userId)
    {
        await SendWebSocketMessageAsync(userId, new WebSocketMessage
        {
            Type = MsgType.CancelRematchRequest,
            Id = userId,
            Content = "CancelRequestRematch"
        });
    }
}