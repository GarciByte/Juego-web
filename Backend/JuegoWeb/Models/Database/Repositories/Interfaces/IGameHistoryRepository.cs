using JuegoWeb.Models.Database.Entities;

namespace JuegoWeb.Models.Database.Repositories.Interfaces;

public interface IGameHistoryRepository
{
    Task<GameHistory> InsertGameHistoryAsync(GameHistory newHistory);
    Task<List<GameHistory>> GetHistoriesByUserIdAsync(int userId);
}