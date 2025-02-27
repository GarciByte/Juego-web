using System.Text.Json.Serialization;

namespace JuegoWeb.Models.Dtos;

public class UserProfileDto
{
    public int UserId { get; set; }

    public string Nickname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public ImageDto Avatar { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsBanned { get; set; }

    public string Password { get; set; } = null!;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserStatus Status { get; set; }
}
