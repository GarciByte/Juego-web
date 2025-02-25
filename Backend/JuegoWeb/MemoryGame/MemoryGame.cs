namespace JuegoWeb.MemoryGame;

// Representa la partida
public class MemoryGame
{
    public long RoomId { get; set; }

    public List<MemoryPlayer> Players { get; set; } = []; // Jugadores

    public List<MemoryCard> Cards { get; set; } = []; // Tablero de cartas

    public int CurrentPlayerIndex { get; set; } // El jugador del turno actual

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public bool GameFinished { get; set; } = false;

    // Cancelar el temporizador del turno
    public CancellationTokenSource TurnCancellationTokenSource { get; set; } = new CancellationTokenSource();

    // Cartas volteadas durante el turno actual
    public List<MemoryCard> CurrentFlippedCards { get; set; } = [];
}