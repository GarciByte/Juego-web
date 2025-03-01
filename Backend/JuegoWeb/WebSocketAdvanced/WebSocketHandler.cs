using JuegoWeb.Models.Dtos;
using JuegoWeb.Services;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace JuegoWeb.WebSocketAdvanced;

public class WebSocketHandler : IDisposable
{
    private const int BUFFER_SIZE = 4096;
    private readonly WebSocket _webSocket;
    private readonly byte[] _buffer;
    private readonly IServiceProvider _serviceProvider;

    public UserDto User { get; private set; }
    public int Id { get; init; }
    public bool IsOpen => _webSocket.State == WebSocketState.Open;

    // Eventos para notificar cuando se recibe un mensaje o se desconecta un usuario
    public event Func<WebSocketHandler, WebSocketMessage, Task> MessageReceived;
    public event Func<WebSocketHandler, Task> Disconnected;

    public WebSocketHandler(int id, WebSocket webSocket, UserDto user, IServiceProvider serviceProvider)
    {
        Id = id;
        _webSocket = webSocket;
        _buffer = new byte[BUFFER_SIZE];
        User = user;
        _serviceProvider = serviceProvider;
    }

    public async Task HandleAsync()
    {
        try
        {
            // Mientras que el websocket esté conectado
            while (IsOpen)
            {
                // Leemos el mensaje
                WebSocketMessage message = await ReadAsync();

                // Si hay mensaje y hay suscriptores al evento MessageReceived, gestionamos el evento
                if (message != null && MessageReceived != null)
                {
                    await MessageReceived.Invoke(this, message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en la conexión de {User.Nickname}: {ex.Message}");
        }
        finally
        {
            // Si hay suscriptores al evento Disconnected, gestionamos el evento
            if (Disconnected != null)
            {
                await Disconnected.Invoke(this);
            }
        }
    }

    private async Task<WebSocketMessage> ReadAsync()
    {
        // Creamos un MemoryStream para almacenar el contenido del mensaje
        using MemoryStream textStream = new MemoryStream();
        WebSocketReceiveResult receiveResult;

        do
        {
            // Recibimos el mensaje
            receiveResult = await _webSocket.ReceiveAsync(_buffer, CancellationToken.None);

            // Si el mensaje es de tipo texto, lo escribimos en el MemoryStream
            if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                textStream.Write(_buffer, 0, receiveResult.Count);
            }
            // Si el mensaje es de tipo Close, cerramos la conexión
            else if (receiveResult.CloseStatus.HasValue)
            {
                await _webSocket.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
            }
        }
        while (!receiveResult.EndOfMessage);

        // Decodificamos el mensaje
        string jsonMessage = Encoding.UTF8.GetString(textStream.ToArray());

        //Console.WriteLine($"Mensaje recibido de {User.Nickname}: {jsonMessage}");

        try
        {
            if (string.IsNullOrEmpty(jsonMessage))
            {
                return null;
            }

            return JsonSerializer.Deserialize<WebSocketMessage>(jsonMessage);

        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error al deserializar el mensaje de {User.Nickname}: {ex.Message}");
            return null;
        }
    }

    public async Task SendAsync(WebSocketMessage message)
    {
        // Si el websocket está abierto, enviamos el mensaje
        if (IsOpen)
        {
            string jsonMessage = JsonSerializer.Serialize(message);

            //Console.WriteLine($"Enviando mensaje para {User.Nickname}: {jsonMessage}");

            // Convertimos el mensaje a bytes y lo enviamos
            byte[] bytes = Encoding.UTF8.GetBytes(jsonMessage);
            await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        else
        {
            Console.WriteLine($"No se ha podido enviar a {User.Nickname} el mensaje.");
        }
    }

    // Obtener todos los amigos del usuario
    public async Task<List<UserDto>> GetFriendsAsync(int userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var friendRequestService = scope.ServiceProvider.GetRequiredService<FriendRequestService>();
        var friends = await friendRequestService.GetFriendsAsync(userId);

        return friends;
    }

    // Cerrar el WebSocket
    public void Dispose()
    {
        _webSocket.Dispose();
    }
}