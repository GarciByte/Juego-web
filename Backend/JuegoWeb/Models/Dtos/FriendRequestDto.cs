namespace JuegoWeb.Models.Dtos;

public class FriendRequestDto
{
    public int Id { get; set; }

    public int SenderId { get; set; }

    public string SenderNickname { get; set; }

    public int ReceiverId { get; set; }

    public string ReceiverNickname { get; set; }

    public bool IsAccepted { get; set; }
}