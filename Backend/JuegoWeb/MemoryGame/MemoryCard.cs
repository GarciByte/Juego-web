namespace JuegoWeb.MemoryGame;

// Representa una carta del juego
public class MemoryCard 
{
    public int CardId { get; set; }

    public int Value { get; set; }       // Valor de la carta

    public bool IsFaceUp { get; set; }   // Si la carta se encuentra volteada

    public bool IsMatched { get; set; }  // Si la carta ya se ha emparejado
}