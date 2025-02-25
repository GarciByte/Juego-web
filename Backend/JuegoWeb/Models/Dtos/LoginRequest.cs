namespace JuegoWeb.Models.Dtos;

public class LoginRequest
{
    public string Nickname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;
}