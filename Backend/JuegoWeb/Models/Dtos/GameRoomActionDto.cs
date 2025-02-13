using System.Text.Json.Serialization;

namespace JuegoWeb.Models.Dtos;

public enum RoomAction
{
    Bot,
    Random,
    Friend,
    CancelRandom,
    CancelRoom,
    StartGame
}

public class GameRoomActionDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RoomAction Action { get; set; }

    public int? FriendId { get; set; }
}