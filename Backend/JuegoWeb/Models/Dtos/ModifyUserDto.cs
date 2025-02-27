namespace JuegoWeb.Models.Dtos;

public class ModifyUserDto
{
    public int UserId { get; set; }

    public string Nickname { get; set; }

    public string Email { get; set; }

    public bool RemoveAvatar { get; set; } = false;

    public IFormFile AvatarFile { get; set; } = null!;
}
