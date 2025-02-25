namespace JuegoWeb.MemoryGame;

// Representa a un jugador en la partida
public class MemoryPlayer
{
    public int UserId { get; set; }

    public int Score { get; set; }

    public bool IsBot { get; set; } = false;

    public bool RequestRematch { get; set; } = false;
}