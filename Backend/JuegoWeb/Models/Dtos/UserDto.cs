using System.Text.Json.Serialization;

namespace JuegoWeb.Models.Dtos;

public enum UserStatus
{
    Online,
    Offline,
    Playing
}

public class UserDto
{
    public int UserId { get; set; }

    public string Nickname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public ImageDto Avatar { get; set; } = null!;

    public string Role { get; set; } = null!;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserStatus Status { get; set; }
}