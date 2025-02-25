using System.Text.Json.Serialization;

namespace JuegoWeb.Models.Dtos;

public enum GameRoomType
{
    Bot,
    Random,
    Friend
}

public class GameRoomDto
{
    public long RoomId { get; set; }

    public int HostUserId { get; set; }

    public int? GuestUserId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GameRoomType RoomType { get; set; }
}