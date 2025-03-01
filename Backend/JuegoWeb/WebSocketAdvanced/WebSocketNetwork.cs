using JuegoWeb.MemoryGame;
using JuegoWeb.Models.Dtos;
using JuegoWeb.Services;
using System.Net.WebSockets;
using System.Text.Json;

namespace JuegoWeb.WebSocketAdvanced;

public class WebSocketNetwork : IWebSocketMessageSender
{
    private readonly IServiceProvider _serviceProvider;

    private readonly GameRoomService _gameRoomService;

    private readonly WebSocketNotificationService _webSocketNotificationService;

    private readonly MemoryGameService _memoryGameService;

    // Lista de WebSocketHandler (clase que gestiona cada WebSocket)
    private readonly List<WebSocketHandler> _handlers = new List<WebSocketHandler>();

    // Lista de invitaciones a partidas
    private readonly List<GameInvitationDto> _gameInvitations = new List<GameInvitationDto>();

    // Semáforo para controlar el acceso a la lista de WebSocketHandler
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    // Diccionario para almacenar los CancellationTokenSource asociados a cada usuario
    private Dictionary<int, CancellationTokenSource> cancellationTokens = new Dictionary<int, CancellationTokenSource>();

    public WebSocketNetwork(
        WebSocketNotificationService webSocketNotificationService,
        GameRoomService gameRoomService,
        MemoryGameService memoryGameService,
        IServiceProvider serviceProvider)
    {
        _webSocketNotificationService = webSocketNotificationService;
        _gameRoomService = gameRoomService;
        _memoryGameService = memoryGameService;
        _serviceProvider = serviceProvider;
    }

    public async Task HandleAsync(WebSocket webSocket, UserDto user)
    {
        //Console.WriteLine("Conexión establecida con " + user.Nickname);

        // Creamos un nuevo WebSocketHandler a partir del WebSocket recibido y lo añadimos a la lista
        WebSocketHandler handler = await AddWebSocketAsync(webSocket, user);

        if (handler != null)
        {
            // El estado del usuario pasa a ser online
            handler.User.Status = UserStatus.Online;

            // Mensaje de conexión exitosa
            await handler.SendAsync(new WebSocketMessage
            {
                Type = MsgType.Connection,
                Id = handler.Id,
                Content = "Conexión establecida con éxito."
            });

            // Esperamos a que el WebSocketHandler termine de manejar la conexión
            await handler.HandleAsync();
        }
    }

    private async Task<WebSocketHandler> AddWebSocketAsync(WebSocket webSocket, UserDto user)
    {
        // Esperamos a que haya un hueco disponible
        await _semaphore.WaitAsync();
        try
        {
            var existingHandler = _handlers.FirstOrDefault(h => h.Id == user.UserId);
            if (existingHandler == null)
            {
                // Creamos un nuevo WebSocketHandler, nos suscribimos a sus eventos y lo añadimos a la lista
                WebSocketHandler handler = new(user.UserId, webSocket, user, _serviceProvider);
                handler.Disconnected += OnDisconnectedAsync;
                handler.MessageReceived += OnMessageReceivedAsync;
                _handlers.Add(handler);

                return handler;
            }
            else
            {
                //Console.WriteLine("El usuario ya tiene una conexión activa.");
                return null;
            }
        }
        finally
        {
            // Liberamos el semáforo
            _semaphore.Release();
        }
    }

    // Notificar el estado del usuario a sus amigos
    private async Task NotifyUserStatusAsync(WebSocketHandler handler)
    {
        List<UserDto> friends = await handler.GetFriendsAsync(handler.Id);

        foreach (var friend in friends)
        {
            await _webSocketNotificationService.NotifyUserStatusToFriendAsync(friend.UserId, handler.Id, handler.User.Status, this);
        }
    }

    // Notificar al usuario sobre el estado de sus amigos
    private async Task NotifyUserFriendsStatusAsync(WebSocketHandler handler)
    {
        await _semaphore.WaitAsync();
        try
        {
            List<UserDto> friends = await handler.GetFriendsAsync(handler.Id);

            var onlineFriends = friends
            .Where(friend => _handlers.Any(h => h.Id == friend.UserId))
            .Select(friend => new
            {
                friend.UserId,
                Status = GetUserStatus(friend.UserId).ToString()
            })
            .ToList();

            var onlineFriendsMessage = new WebSocketMessage
            {
                Type = MsgType.FriendStatusUpdate,
                Id = handler.Id,
                Content = onlineFriends
            };

            await handler.SendAsync(onlineFriendsMessage);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // Obtener el estado actual de un usuario
    private UserStatus GetUserStatus(int userId)
    {
        return _handlers.FirstOrDefault(h => h.Id == userId)?.User.Status ?? UserStatus.Offline;
    }

    // Obtener las estadísticas globales y enviarlas a todos los usuarios
    public async Task NotifyStatsAsync()
    {
        int totalPlayers = 0;
        int activeGames = 0;
        int playersInGames = 0;

        await _semaphore.WaitAsync();

        try
        {
            // Cantidad total de jugadores
            totalPlayers = _handlers.Count;

            // Cantidad de partidas activas en curso
            activeGames = _memoryGameService.GetActiveGameCount();

            // Cantidad total de jugadores que están en partidas activas
            playersInGames = _memoryGameService.GetActivePlayersCount();
        }
        finally
        {
            _semaphore.Release();
        }

        // Notificar las estadísticas globales
        await _webSocketNotificationService.NotifyStatsUpdatedAsync(totalPlayers, activeGames, playersInGames, this);
    }

    // Enviar un mensaje a un usuario en concreto
    public async Task SendToUserAsync(int userId, WebSocketMessage message)
    {
        await _semaphore.WaitAsync();
        try
        {
            var handler = _handlers.FirstOrDefault(h => h.Id == userId);
            if (handler != null && handler.IsOpen)
            {
                await handler.SendAsync(message);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // Enviar un mensaje a todos los usuarios
    public async Task SendToAllAsync(WebSocketMessage message)
    {
        await _semaphore.WaitAsync();
        try
        {
            var tasks = _handlers
                .Where(handler => handler.IsOpen)
                .Select(async handler =>
                {
                    try
                    {
                        await handler.SendAsync(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error enviando un mensaje a {handler.User.Nickname}: {ex.Message}");
                    }
                });
            await Task.WhenAll(tasks);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // Desconexión de un usuario
    private async Task OnDisconnectedAsync(WebSocketHandler handler)
    {
        //Console.WriteLine($"Iniciando desconexión de {handler.User.Nickname}.");

        await _semaphore.WaitAsync();
        try
        {
            // Eliminamos el WebSocketHandler de la lista
            _handlers.Remove(handler);
        }
        finally
        {
            handler.Dispose();
            _semaphore.Release();

            // Eliminarlo de la partida en curso
            var gameRoom = await _gameRoomService.GetRoomByUserIdsAsync(handler.Id);
            if (gameRoom != null)
            {
                await _memoryGameService.DeleteGameAsync(gameRoom.RoomId, handler.Id);
            }

            // Eliminarlo de la búsqueda de oponente aleatorio
            await CancelRandomSearch(handler.Id);

            // Eliminarlo de cualquier sala activa
            var room = await _gameRoomService.HandleUserDisconnectionAsync(handler.Id);
            if (room != null)
            {
                await GameRoomUpdateAsync(room);
            }

            // Eliminar cualquier invitación enviada o recibida
            var existingGameInvitations = _gameInvitations.FindAll(
                                invitation => invitation.FromUserId == handler.Id || invitation.ToUserId == handler.Id);

            if (existingGameInvitations.Count > 0)
            {
                foreach (var existingGameInvitation in existingGameInvitations)
                {
                    if (handler.Id == existingGameInvitation.FromUserId)
                    {
                        await _webSocketNotificationService.NotifyCancelGameInvitationAsync(existingGameInvitation.ToUserId, existingGameInvitation, this);
                    }
                    else
                    {
                        await _webSocketNotificationService.NotifyCancelGameInvitationAsync(existingGameInvitation.FromUserId, existingGameInvitation, this);
                    }
                    await _semaphore.WaitAsync();
                    try
                    {
                        _gameInvitations.Remove(existingGameInvitation);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            }

            // Notificar su nuevo estado
            handler.User.Status = UserStatus.Offline;
            await NotifyUserStatusAsync(handler);

            // Notificar las estadísticas globales
            await NotifyStatsAsync();

            //Console.WriteLine($"Desconexión completada.");
        }
    }

    // Mensajes recibidos por los usuarios
    private async Task OnMessageReceivedAsync(WebSocketHandler handler, WebSocketMessage message)
    {
        try
        {
            switch (message.Type)
            {
                // Notificar los estados de los usuarios
                case MsgType.FriendStatusUpdate:
                    await NotifyUserStatusAsync(handler);
                    await NotifyUserFriendsStatusAsync(handler);
                    break;

                // Notificar las estadísticas globales
                case MsgType.StatsUpdate:
                    await NotifyStatsAsync();
                    break;

                // Gestionar una sala de juego
                case MsgType.GameRoom:
                    await GameRoomCreationAsync(handler, message);
                    break;

                // Invitación de un amigo
                case MsgType.GameInvitation:
                    await GameInvitationAsync(handler, message);
                    break;

                // Cancelar la invitación
                case MsgType.CancelGameInvitation:
                    await CancelGameInvitationAsync(handler, message);
                    break;

                // Mensaje del chat
                case MsgType.Chat:
                    await HandleChatMessageAsync(handler, message);
                    break;

                // Actualizaciones de la partida (el jugador ha volteado una carta)
                case MsgType.GameUpdate:
                    await GameMoveAsync(handler, message);
                    break;

                // Solicitud de revancha en una partida
                case MsgType.RematchRequest:
                    await RequestRematchAsync(handler, message);
                    break;

                // Usuario baneado
                case MsgType.UserBanned:
                    await HandleUserBanAsync(message);
                    break;

                default:
                    //Console.WriteLine($"Mensaje no manejado: {message.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error procesando mensaje: {ex.Message}");
        }
    }

    // Notificar la prohibición de un usuario
    private async Task HandleUserBanAsync(WebSocketMessage message)
    {
        try
        {
            string jsonContent = message.Content.ToString();
            int userId = JsonSerializer.Deserialize<int>(jsonContent);
            //Console.WriteLine($"Se ha baneado al usuaio con ID: {userId}");

            await SendToUserAsync(userId, new WebSocketMessage
            {
                Type = MsgType.UserBanned,
                Id = userId,
                Content = "UserBanned"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al deserializar userId: {ex.Message}");
        }
    }

    // Manejar mensajes del chat
    private async Task HandleChatMessageAsync(WebSocketHandler handler, WebSocketMessage message)
    {
        ChatMessageDto chatMessage = null;

        try
        {
            string jsonContent = message.Content.ToString();
            chatMessage = JsonSerializer.Deserialize<ChatMessageDto>(jsonContent);
            string updatedMessage = $"{chatMessage.Nickname}: {chatMessage.Content}";

            var chatMessageUpdated = new ChatMessageDto
            {
                UserId = chatMessage.UserId,
                Nickname = chatMessage.Nickname,
                FriendId = chatMessage.FriendId,
                Content = updatedMessage
            };

            await SendToUserAsync(chatMessage.FriendId, new WebSocketMessage
            {
                Type = MsgType.Chat,
                Id = chatMessage.FriendId,
                Content = chatMessageUpdated
            });

            var chatResponse = new WebSocketMessage
            {
                Type = MsgType.Chat,
                Id = chatMessage.UserId,
                Content = chatMessageUpdated
            };

            await handler.SendAsync(chatResponse);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al deserializar chatMessage: {ex.Message}");
        }
    }

    // Notificar la invitación a una partida
    private async Task GameInvitationAsync(WebSocketHandler handler, WebSocketMessage message)
    {
        //Console.WriteLine($"Invitación de partida enviada por {handler.User.Nickname}.");
        GameInvitationDto gameInvitation = null;

        try
        {
            string jsonContent = message.Content.ToString();
            gameInvitation = JsonSerializer.Deserialize<GameInvitationDto>(jsonContent);

            // Buscar si al usuario ya lo han invitado o ha invitado a alguien
            var existingGameInvitations = _gameInvitations.FindAll(
                        invitation => invitation.FromUserId == gameInvitation.ToUserId || invitation.ToUserId == gameInvitation.ToUserId);

            if (existingGameInvitations.Count > 0)
            {
                //Console.WriteLine($"Hay invitaciones pendientes para este usuario.");
                await _webSocketNotificationService.NotifyCancelGameInvitationAsync(handler.Id, gameInvitation, this);
            }
            else
            {
                await _webSocketNotificationService.NotifyGameInvitationAsync(gameInvitation.ToUserId, gameInvitation, this);

                await _semaphore.WaitAsync();
                try
                {
                    _gameInvitations.Add(gameInvitation);
                }
                finally
                {
                    _semaphore.Release();
                }
                //Console.WriteLine($"Hay {_gameInvitations.Count} invitaciones pendientes.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al deserializar gameInvitation: {ex.Message}");
        }

    }

    // Notificar la cancelación de una invitación a una partida
    private async Task CancelGameInvitationAsync(WebSocketHandler handler, WebSocketMessage message)
    {
        //Console.WriteLine($"Cancelación de la invitación de partida enviada por {handler.User.Nickname}.");
        GameInvitationDto gameInvitation = null;

        try
        {
            string jsonContent = message.Content.ToString();
            gameInvitation = JsonSerializer.Deserialize<GameInvitationDto>(jsonContent);

            if (handler.Id == gameInvitation.FromUserId)
            {
                await _webSocketNotificationService.NotifyCancelGameInvitationAsync(gameInvitation.ToUserId, gameInvitation, this);
            }
            else
            {
                await _webSocketNotificationService.NotifyCancelGameInvitationAsync(gameInvitation.FromUserId, gameInvitation, this);
            }

            await _semaphore.WaitAsync();
            try
            {
                _gameInvitations.RemoveAll(invitation =>
                    invitation.FromUserId == gameInvitation.FromUserId &&
                    invitation.ToUserId == gameInvitation.ToUserId);
            }
            finally
            {
                _semaphore.Release();
            }
            //Console.WriteLine($"Aun quedan {_gameInvitations.Count} invitaciones pendientes.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al deserializar gameInvitation: {ex.Message}");
        }
    }

    // Solicitud de revancha en una partida
    private async Task RequestRematchAsync(WebSocketHandler handler, WebSocketMessage message)
    {
        GameRoomDto gameRoom = null;
        try
        {
            string jsonContent = message.Content.ToString();
            gameRoom = JsonSerializer.Deserialize<GameRoomDto>(jsonContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al deserializar GameRoomDto: {ex.Message}");
        }

        if (gameRoom != null)
        {
            var activeGameRoom = await _gameRoomService.GetRoomByUserIdsAsync(handler.Id);
            if (activeGameRoom != null)
            {
                bool existingGameRoom = activeGameRoom.RoomId == gameRoom.RoomId;
                if (existingGameRoom)
                {
                    await _memoryGameService.RequestRematchAsync(activeGameRoom, handler.Id);
                }
                else
                {
                    Console.WriteLine($"El usuario {handler.User.Nickname} no está en ninguna sala activa.");
                }
            }
        }
    }

    // Actualizaciones de la partida (el jugador ha volteado una carta)
    private async Task GameMoveAsync(WebSocketHandler handler, WebSocketMessage message)
    {
        MemoryGameMoveDto move = null;
        try
        {
            string jsonContent = message.Content.ToString();
            move = JsonSerializer.Deserialize<MemoryGameMoveDto>(jsonContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al deserializar MemoryGameMoveDto: {ex.Message}");
        }

        if (move != null)
        {
            await _memoryGameService.ProcessMoveAsync(move.RoomId, handler.Id, move);
        }
    }

    // Notificar la actualización de una sala
    private async Task GameRoomUpdateAsync(GameRoomDto room)
    {
        await _webSocketNotificationService.NotifyRoomUpdatedAsync(room.HostUserId, room, this);
    }

    // Gestiona una sala de juego del tipo seleccionado
    private async Task GameRoomCreationAsync(WebSocketHandler handler, WebSocketMessage message)
    {
        try
        {
            GameRoomActionDto gameAction = null;

            try
            {
                string jsonContent = message.Content.ToString();
                gameAction = JsonSerializer.Deserialize<GameRoomActionDto>(jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al deserializar GameRoomActionDto: {ex.Message}");
            }

            switch (gameAction.Action)
            {
                // Crear una sala para jugar contra un bot
                case RoomAction.Bot:
                    GameRoomDto room = await _gameRoomService.CreateRoomAgainstBotAsync(handler.Id);

                    var response = new WebSocketMessage
                    {
                        Type = MsgType.GameRoom,
                        Id = handler.Id,
                        Content = room
                    };

                    await handler.SendAsync(response);
                    break;

                // Crear una sala para jugar contra un oponente aleatorio
                case RoomAction.Random:
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cancellationTokens[handler.Id] = cts;

                    _ = Task.Run(async () =>
                    {
                        GameRoomDto room = await _gameRoomService.SearchRandomOpponentAsync(handler.Id, cts.Token);
                        if (room != null)
                        {
                            var roomResponse = new WebSocketMessage
                            {
                                Type = MsgType.GameRoom,
                                Id = handler.Id,
                                Content = room
                            };

                            await handler.SendAsync(roomResponse);
                        }
                    });
                    break;

                // Crear una sala para jugar contra un amigo
                case RoomAction.Friend:
                    if (gameAction.FriendId.HasValue)
                    {
                        int hostUserId = gameAction.FriendId.Value;
                        var handlerOnline = _handlers.FirstOrDefault(h => h.Id == hostUserId);

                        if (handlerOnline != null && handlerOnline.IsOpen)
                        {
                            GameRoomDto gameRoomFriend = await _gameRoomService.CreateRoomByInvitationAsync(hostUserId, handler.Id);

                            // Enviar sala al anfitrión
                            await SendToUserAsync(hostUserId, new WebSocketMessage
                            {
                                Type = MsgType.GameRoom,
                                Id = hostUserId,
                                Content = gameRoomFriend
                            });

                            // Enviar sala al usuario invitado
                            var roomResponse = new WebSocketMessage
                            {
                                Type = MsgType.GameRoom,
                                Id = handler.Id,
                                Content = gameRoomFriend
                            };

                            await handler.SendAsync(roomResponse);
                        }
                        else
                        {
                            Console.WriteLine($"No se puede crear la partida porque el anfitrión abandonó la sala.");
                        }

                        await _semaphore.WaitAsync();
                        try
                        {
                            var existingGameInvitation = _gameInvitations.FirstOrDefault(
                                invitation => invitation.FromUserId == hostUserId && invitation.ToUserId == handler.Id);

                            if (existingGameInvitation != null)
                            {
                                _gameInvitations.Remove(existingGameInvitation);
                            }
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                        //Console.WriteLine($"Quedan {_gameInvitations.Count} invitaciones pendientes.");
                    }
                    break;

                // Cancelar la búsqueda aleatoria
                case RoomAction.CancelRandom:
                    await CancelRandomSearch(handler.Id);
                    break;

                // Elimina al usuario de la sala en la que se encuentra
                case RoomAction.CancelRoom:
                    var activeGameRoom = await _gameRoomService.GetRoomByUserIdsAsync(handler.Id);
                    if (activeGameRoom != null)
                    {
                        await _memoryGameService.DeleteGameAsync(activeGameRoom.RoomId, handler.Id);
                    }

                    var roomUpdated = await _gameRoomService.HandleUserDisconnectionAsync(handler.Id);
                    if (roomUpdated != null)
                    {
                        await GameRoomUpdateAsync(roomUpdated);
                    }

                    // Notificar el nuevo estado del usuario
                    handler.User.Status = UserStatus.Online;
                    await NotifyUserStatusAsync(handler);
                    break;

                // Comienza la partida de ambos usuarios
                case RoomAction.StartGame:
                    GameRoomDto gameRoom = await _gameRoomService.GetRoomByUserIdsAsync(handler.Id);

                    if (gameRoom != null)
                    {
                        if (gameAction.FriendId != null)
                        {
                            int opponentId = (int)gameAction.FriendId;

                            // Enviar notificación al usuario invitado
                            await SendToUserAsync(opponentId, new WebSocketMessage
                            {
                                Type = MsgType.StartGame,
                                Id = opponentId,
                                Content = gameAction.Action.ToString()
                            });

                            var existingHandler = _handlers.FirstOrDefault(h => h.Id == opponentId);
                            if (existingHandler != null)
                            {
                                // Notificar el nuevo estado del usuario invitado
                                existingHandler.User.Status = UserStatus.Playing;
                                await NotifyUserStatusAsync(existingHandler);
                            }
                        }

                        // Crea la partida
                        _memoryGameService.CreateGame(gameRoom);

                        // Notificar las estadísticas globales
                        await NotifyStatsAsync();

                        // Notificar el nuevo estado del usuario
                        handler.User.Status = UserStatus.Playing;
                        await NotifyUserStatusAsync(handler);
                    }
                    else
                    {
                        Console.WriteLine($"El usuario {handler.User.Nickname} no se encuentra en una sala.");
                    }
                    break;

                default:
                    //Console.WriteLine($"Acción de juego desconocida: {gameAction.Action}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GameRoomCreationAsync: {ex.Message}");
        }
    }

    // Cancela la búsqueda aleatoria de oponente para un usuario 
    public async Task CancelRandomSearch(int userId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (cancellationTokens.TryGetValue(userId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                cancellationTokens.Remove(userId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}