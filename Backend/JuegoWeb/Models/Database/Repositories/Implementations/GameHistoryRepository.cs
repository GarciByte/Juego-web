using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Database.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JuegoWeb.Models.Database.Repositories.Implementations;

public class GameHistoryRepository : Repository<GameHistory, int>, IGameHistoryRepository
{
    public GameHistoryRepository(JuegoWebContext context) : base(context) { }

    // Guardar el resultado de una partida
    public async Task<GameHistory> InsertGameHistoryAsync(GameHistory newHistory)
    {
        await base.InsertAsync(newHistory);
        return newHistory;
    }

    // Obtener historial de partidas de un usuario
    public async Task<List<GameHistory>> GetHistoriesByUserIdAsync(int userId)
    {
        return await GetQueryable()
            .Where(history => history.UserId == userId)
            .ToListAsync();
    }
}