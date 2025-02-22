using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Dtos;

namespace JuegoWeb.Models.Mappers;

public class GameHistoryMapper
{
    public GameHistoryDto GameHistoryToDto(GameHistory history)
    {
        return new GameHistoryDto
        {
            Id = history.Id,
            GameName = history.GameName,
            Score = history.Score,
            Players = history.Players,
            Result = history.Result,
            Duration = history.Duration,
            UserId = history.UserId
        };
    }

    public IEnumerable<GameHistoryDto> HistoriesToDto(IEnumerable<GameHistory> histories)
    {
        return histories.Select(history => GameHistoryToDto(history));
    }
}