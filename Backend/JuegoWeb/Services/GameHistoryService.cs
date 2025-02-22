using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Database;
using JuegoWeb.Models.Dtos;
using JuegoWeb.Models.Mappers;

namespace JuegoWeb.Services;

public class GameHistoryService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly GameHistoryMapper _gameHistoryMapper;

    public GameHistoryService(UnitOfWork unitOfWork, GameHistoryMapper gameHistoryMapper)
    {
        _unitOfWork = unitOfWork;
        _gameHistoryMapper = gameHistoryMapper;
    }

    // Guardar el resultado de una partida
    public async Task<GameHistoryDto> AddGameHistoryAsync(GameHistory gameHistory)
    {
        await _unitOfWork.IGameHistoryRepository.InsertGameHistoryAsync(gameHistory);
        await _unitOfWork.SaveAsync();

        return _gameHistoryMapper.GameHistoryToDto(gameHistory);
    }

    // Obtener historial de partidas de un usuario
    public async Task<List<GameHistoryDto>> GetHistoriesByUserIdAsync(int userId)
    {
        var histories = await _unitOfWork.IGameHistoryRepository.GetHistoriesByUserIdAsync(userId);

        return _gameHistoryMapper.HistoriesToDto(histories).ToList();
    }
}