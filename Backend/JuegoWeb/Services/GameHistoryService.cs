using JuegoWeb.Models.Database;
using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Dtos;
using JuegoWeb.Models.Mappers;

namespace JuegoWeb.Services;

public class GameHistoryService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly GameHistoryMapper _gameHistoryMapper;
    private readonly UserService _userService;

    public GameHistoryService(UnitOfWork unitOfWork, GameHistoryMapper gameHistoryMapper, UserService userService)
    {
        _unitOfWork = unitOfWork;
        _gameHistoryMapper = gameHistoryMapper;
        _userService = userService;
    }

    // Guardar el resultado de una partida
    public async Task<GameHistoryDto> AddGameHistoryAsync(GameHistory gameHistory)
    {
        string[] playerIds = gameHistory.Players.Split(',');
        string playerNickname1 = "";
        string playerNickname2 = "";

        if (playerIds.Length == 2 && int.TryParse(playerIds[0], out int player1) && int.TryParse(playerIds[1], out int player2))
        {
            var user = await _userService.GetUserByIdAsync(player1, null);
            playerNickname1 = user.Nickname;

            if (player2 == -1)
            {
                playerNickname2 = "Bot";
            }
            else
            {
                var user2 = await _userService.GetUserByIdAsync(player2, null);
                playerNickname2 = user2.Nickname;
            }
        }

        gameHistory.Players = ($"{playerNickname1}, {playerNickname2}");

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