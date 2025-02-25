namespace JuegoWeb.Models.Dtos;

public class ChatMessageDto
{
    public int UserId { get; set; }

    public string Nickname { get; set; }

    public int FriendId { get; set; }

    public string Content { get; set; }
}