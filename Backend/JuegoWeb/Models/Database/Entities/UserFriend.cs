using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JuegoWeb.Models.Database.Entities;

public class UserFriend
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    public int FriendId { get; set; }

    [ForeignKey("FriendId")]
    public User Friend { get; set; }
}