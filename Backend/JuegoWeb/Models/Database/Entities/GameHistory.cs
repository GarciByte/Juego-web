using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JuegoWeb.Models.Database.Entities;

[Index(nameof(UserId))]
public class GameHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string GameName { get; set; } = null!;

    public int Score { get; set; }

    public int OpponentScore { get; set; }

    public string Players { get; set; } = null!;

    public string Result { get; set; } = null!;

    public TimeSpan Duration { get; set; }

    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}