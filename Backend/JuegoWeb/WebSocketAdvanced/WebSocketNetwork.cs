using JuegoWeb.Models.Dtos;
using System.Net.WebSockets;
using System.Text.Json;

namespace JuegoWeb.WebSocketAdvanced;

public class WebSocketNetwork : IWebSocketMessageSender
{
    private readonly WebSocketNotificationService _webSocketNotificationService;

    // Lista de WebSocketHandler (clase que gestiona cada WebSocket)
    private readonly List<WebSocketHandler> _handlers = new List<WebSocketHandler>();

    // Semáforo para controlar el acceso a la lista de WebSocketHandler
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public WebSocketNetwork(WebSocketNotificationService webSocketNotificationService)
    {
        _webSocketNotificationService = webSocketNotificationService;
    }

    public async Task HandleAsync(WebSocket webSocket, UserDto user, List<UserDto> friends)
    {
        Console.WriteLine("Conexión establecida con " + user.Nickname);

        // Creamos un nuevo WebSocketHandler a partir del WebSocket recibido y lo añadimos a la lista
        WebSocketHandler handler = await AddWebSocketAsync(webSocket, user, friends);

        // Mensaje de conexión exitosa
        await handler.SendAsync(new WebSocketMessage
        {
            Type = MsgType.Connection,
            Id = handler.Id,
            Content = "Conexión establecida con éxito."
        });

        // Notificar a los amigos sobre el estado del usuario
        await NotifyUserStatusAsync(handler);

        // Notificar al usuario sobre el estado de sus amigos
        await NotifyUserFriendsStatusAsync(handler);

        // Notificar las estadísticas globales
        await NotifyStatsAsync();

        // Esperamos a que el WebSocketHandler termine de manejar la conexión
        await handler.HandleAsync();
    }

    private async Task<WebSocketHandler> AddWebSocketAsync(WebSocket webSocket, UserDto user, List<UserDto> friends)
    {
        // Esperamos a que haya un hueco disponible
        await _semaphore.WaitAsync();
        try
        {
            // Creamos un nuevo WebSocketHandler, nos suscribimos a sus eventos y lo añadimos a la lista
            WebSocketHandler handler = new(user.UserId, webSocket, user, friends);
            handler.Disconnected += OnDisconnectedAsync;
            handler.MessageReceived += OnMessageReceivedAsync;
            _handlers.Add(handler);

            return handler;
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

    // Obtener el estado actual de un usuario
    private UserStatus GetUserStatus(int userId)
    {
        var handler = _handlers.FirstOrDefault(h => h.Id == userId);

        if (handler != null)
        {
            Console.WriteLine($"El usuario {handler.User.Nickname} tiene un estado de: {handler.User.Status}.");
            return handler.User.Status;
        }

        return UserStatus.Offline;
    }

    // Obtener las estadísticas globales y enviarlas a todos los usuarios
    public async Task NotifyStatsAsync()
    {
        // Cantidad total de jugadores
        int totalPlayers = _handlers.Count;

        int activeGames = 0;        // Por implementar
        int playersInGames = 0;     // Por implementar

        // Notificar las estadísticas globales
        await _webSocketNotificationService.NotifyStatsUpdatedAsync(totalPlayers, activeGames, playersInGames, this);
    }

    // Enviar un mensaje a un usuario en concreto
    public async Task SendToUserAsync(int userId, WebSocketMessage message)
    {
        Console.WriteLine($"Método SendToUserAsync ejecutado para este mensaje: {JsonSerializer.Serialize(message)}");   
        var handler = _handlers.FirstOrDefault(h => h.Id == userId);

        if (handler != null)
        {
            await handler.SendAsync(message);
        }
        else
        {
            Console.WriteLine("No está el usuario al que envirle el mensaje.");
        }
    }

    // Enviar un mensaje a todos los usuarios
    public async Task SendToAllAsync(WebSocketMessage message)
    {
        var tasks = _handlers.Select(handler => handler.SendAsync(message));
        await Task.WhenAll(tasks);
    }

    // Desconexión de un usuario
    private async Task OnDisconnectedAsync(WebSocketHandler handler)
    {
        Console.WriteLine($"El usuario {handler.Id} se ha desconectado.");

        // Notificar el nuevo estado del usuario a sus amigos
        foreach (var friend in handler.Friends)
        {
            await _webSocketNotificationService.NotifyUserStatusToFriendAsync(friend.UserId, handler.Id, UserStatus.Offline, this);
        }

        // Eliminamos el WebSocketHandler de la lista
        _handlers.Remove(handler);
        handler.Dispose();

        // Notificar las estadísticas globales
        await NotifyStatsAsync();
    }

    // Mensajes recibidos por los usuarios
    private Task OnMessageReceivedAsync(WebSocketHandler handler, WebSocketMessage message)
    {
        // Por implementar

        Console.WriteLine($"Mensaje recibido de {handler.User.Nickname}: {JsonSerializer.Serialize(message)}");

        return null;
    }
}