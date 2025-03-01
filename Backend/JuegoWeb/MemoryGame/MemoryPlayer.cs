namespace JuegoWeb.MemoryGame;

// Representa a un jugador en la partida
public class MemoryPlayer
{
    public int UserId { get; set; }

    public int Score { get; set; } // Puntuación del jugador

    public bool IsBot { get; set; } = false; // Si el jugador es un bot

    public bool RequestRematch { get; set; } = false; // Si el jugador quiere una revancha
}