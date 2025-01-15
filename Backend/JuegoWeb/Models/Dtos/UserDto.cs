using JuegoWeb.Models.Database.Entities;

namespace JuegoWeb.Models.Dtos;

public class UserDto
{
    public int UserId { get; set; }

    public string Nickname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public ImageDto Avatar { get; set; } = null!;

    public string Role { get; set; } = null!;
}
