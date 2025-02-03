using System.Text.Json.Serialization;

namespace JuegoWeb.Models.Dtos;

public enum MsgType
{
    Connection,             // Conexión con el WebSocket
    FriendListUpdate,       // Actualizar la lista de amigos
    FriendStatusUpdate,     // Actualizar estados de los amigos
    FriendRequestUpdate,    // Notificar de solicitudes de amistad
    GameInvitation,         // Invitaciones a partidas
    StatsUpdate,            // Estadísticas globales
}

public class WebSocketMessage
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MsgType Type { get; set; }

    public int Id { get; set; }

    public object Content { get; set; }
}