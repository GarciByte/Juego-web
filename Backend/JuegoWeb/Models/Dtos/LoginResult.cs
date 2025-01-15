namespace JuegoWeb.Models.Dtos;

public class LoginResult
{
    public string AccessToken { get; set; }

    public UserDto User { get; set; }
}
