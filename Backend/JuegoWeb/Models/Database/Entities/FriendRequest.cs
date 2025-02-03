using System.ComponentModel.DataAnnotations.Schema;

namespace JuegoWeb.Models.Database.Entities;

public class FriendRequest
{
    public int Id { get; set; }

    [ForeignKey("Sender")]
    public int SenderId { get; set; }

    [ForeignKey("Receiver")]
    public int ReceiverId { get; set; }

    public bool IsAccepted { get; set; }

    public virtual User Sender { get; set; }

    public virtual User Receiver { get; set; }
}
