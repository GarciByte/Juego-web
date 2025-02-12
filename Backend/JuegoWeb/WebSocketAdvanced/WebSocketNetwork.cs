using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Dtos;
using JuegoWeb.Services;
using System.Net.WebSockets;
using System.Text.Json;

namespace JuegoWeb.WebSocketAdvanced;

public class WebSocketNetwork : IWebSocketMessageSender
{
    private readonly GameRoomService _gameRoomService;

    private readonly WebSocketNotificationService _webSocketNotificationService;

    // Lista de WebSocketHandler (clase que gestiona cada WebSocket)
    private readonly List<WebSocketHandler> _handlers = new List<WebSocketHandler>();

    // Semáforo para controlar el acceso a la lista de WebSocketHandler
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    // Diccionario para almacenar los CancellationTokenSource asociados a cada usuario
    private Dictionary<int, CancellationTokenSource> cancellationTokens = new Dictionary<int, CancellationTokenSource>();

    public WebSocketNetwork(WebSocketNotificationService webSocketNotificationService, GameRoomService gameRoomService)
    {
        _webSocketNotificationService = webSocketNotificationService;
        _gameRoomService = gameRoomService;
    }

    public async Task HandleAsync(WebSocket webSocket, UserDto user, List<UserDto> friends)
    {
        Console.WriteLine("Conexión establecida con " + user.Nickname);

        // Creamos un nuevo WebSocketHandler a partir del WebSocket recibido y lo añadimos a la lista
        WebSocketHandler handler = await AddWebSocketAsync(webSocket, user, friends);

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

    private async Task<WebSocketHandler> AddWebSocketAsync(WebSocket webSocket, UserDto user, List<UserDto> friends)
    {
        // Esperamos a que haya un hueco disponible
        await _semaphore.WaitAsync();
        try
        {
            var existingHandler = _handlers.FirstOrDefault(h => h.Id == user.UserId);
            if (existingHandler == null)
            {
                // Creamos un nuevo WebSocketHandler, nos suscribimos a sus eventos y lo añadimos a la lista
                WebSocketHandler handler = new(user.UserId, webSocket, user, friends);
                handler.Disconnected += OnDisconnectedAsync;
                handler.MessageReceived += OnMessageReceivedAsync;
                _handlers.Add(handler);

                return handler;
            }
            else
            {
                Console.WriteLine("El usuario ya tiene una conexión activa.");
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
        foreach (var friend in handler.Friends)
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
            var onlineFriends = handler.Friends
            .Where(friend => _handlers.Any(h => h.Id == friend.UserId))
            .Select(friend => new
            {
                UserId = friend.UserId,
                Status = GetUserStatus(friend.UserId).ToString()
            });

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
        Console.WriteLine($"Iniciando desconexión de {handler.User.Nickname}.");

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
           
            
            Console.WriteLine($"Eliminándolo de salas activas.");

            // Eliminarlo de cualquier sala activa
            var room = await _gameRoomService.HandleUserDisconnectionAsync(handler.Id);
            if (room != null) 
            {
                await GameRoomUpdateAsync(room);
            }

            Console.WriteLine($"Enviando notificaciones.");

            // Notificar a amigos
            foreach (var friend in handler.Friends)
            {
                await _webSocketNotificationService.NotifyUserStatusToFriendAsync(
                    friend.UserId,
                    handler.Id,
                    UserStatus.Offline,
                    this
                );
            }
            Console.WriteLine($"Notificaciones a amigos enviadas.");

            // Notificar las estadísticas globales
            await NotifyStatsAsync();
            Console.WriteLine($"Notificaciones globales enviadas.");

            Console.WriteLine($"Desconexión completada.");
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

                // Invitación a un amigo
                case MsgType.GameInvitation:
                    await GameInvitationAsync(handler, message);
                    break;

                default:
                    Console.WriteLine($"Mensaje no manejado: {message.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error procesando mensaje: {ex.Message}");
        }
    }

    // Notificar la invitación a una partida
    private async Task GameInvitationAsync(WebSocketHandler handler, WebSocketMessage message)
    {
        GameInvitationDto gameInvitation = null;

        try
        {
            string jsonContent = message.Content.ToString();
            gameInvitation = JsonSerializer.Deserialize<GameInvitationDto>(jsonContent);
            await _webSocketNotificationService.NotifyGameInvitationAsync(gameInvitation.ToUserId, gameInvitation, this);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al deserializar gameInvitation: {ex.Message}");
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
                    // CancellationTokenSource para cancelar la búsqueda
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
                        GameRoomDto gameRoom = await _gameRoomService.CreateRoomByInvitationAsync(hostUserId, handler.Id);

                        // Enviar sala al anfitrión
                        await SendToUserAsync(hostUserId, new WebSocketMessage
                        {
                            Type = MsgType.GameRoom,
                            Id = hostUserId,
                            Content = gameRoom
                        });

                        // Enviar sala al usuario invitado
                        var roomResponse = new WebSocketMessage
                        {
                            Type = MsgType.GameRoom,
                            Id = handler.Id,
                            Content = gameRoom
                        };

                        await handler.SendAsync(roomResponse);
                    }
                    break;

                // Cancelar la búsqueda aleatoria
                case RoomAction.CancelRandom:
                    await CancelRandomSearch(handler.Id);
                    Console.WriteLine($"Búsqueda cancelada para: {handler.User.Nickname}");
                    break;

                // Elimina al usuario de la sala en la que se encuentra
                case RoomAction.CancelRoom:
                    var roomUpdated = await _gameRoomService.HandleUserDisconnectionAsync(handler.Id);
                    if (roomUpdated != null)
                    {
                        await GameRoomUpdateAsync(roomUpdated);
                    }
                    Console.WriteLine($"Sala eliminada para: {handler.User.Nickname}");
                    break;

                default:
                    Console.WriteLine($"Acción de juego desconocida: {gameAction.Action}");
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