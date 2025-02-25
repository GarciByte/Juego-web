namespace JuegoWeb.Models.Dtos;

public class GameHistoryDto
{
    public int Id { get; set; }

    public string GameName { get; set; }

    public int Score { get; set; }

    public int OpponentScore { get; set; }

    public string Players { get; set; }

    public string Result { get; set; }

    public TimeSpan Duration { get; set; }

    public int UserId { get; set; }
}