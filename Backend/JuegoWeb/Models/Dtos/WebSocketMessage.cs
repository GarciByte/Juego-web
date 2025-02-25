using System.Text.Json.Serialization;

namespace JuegoWeb.Models.Dtos;

public enum MsgType
{
    Connection,             // Conexión con el WebSocket
    FriendListUpdate,       // Actualizar la lista de amigos
    FriendStatusUpdate,     // Actualizar estados de los amigos
    FriendRequestUpdate,    // Notificar de solicitudes de amistad
    GameRoom,               // Gestionar una sala de juego
    StartGame,              // Notificar del inicio de la partida al usuario invitado
    GameInvitation,         // Invitación de un amigo
    CancelGameInvitation,   // Cancelar la invitación
    StatsUpdate,            // Estadísticas globales
    GameStart,              // Comenzarla partida
    GameUpdate,             // Actualizaciones de la partida
    GameOver,               // Finalizar partida
    Chat,                   // Mensaje del chat
    RematchRequest,         // Solicitud de revancha
    CancelRematchRequest    // Cancelar revancha
}

public class WebSocketMessage
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MsgType Type { get; set; }

    public int Id { get; set; }

    public object Content { get; set; }
}