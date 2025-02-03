using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace JuegoWeb.Models.Database.Entities;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Nickname), IsUnique = true)]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Nickname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;

    [ForeignKey("AvatarId")]
    public virtual Image Avatar { get; set; }

    public int AvatarId { get; set; }

    [InverseProperty("Sender")]
    public virtual ICollection<FriendRequest> SentRequests { get; set; }

    [InverseProperty("Receiver")]
    public virtual ICollection<FriendRequest> ReceivedRequests { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<UserFriend> Friends { get; set; }
}
