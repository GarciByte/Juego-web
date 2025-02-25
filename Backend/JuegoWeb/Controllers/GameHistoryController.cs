using JuegoWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace JuegoWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameHistoryController : ControllerBase
{
    private readonly GameHistoryService _gameHistoryService;

    public GameHistoryController(GameHistoryService gameHistoryService)
    {
        _gameHistoryService = gameHistoryService;
    }

    // Obtener historial de partidas de un usuario
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetGameHistoriesByUser(int userId)
    {
        try
        {
            var histories = await _gameHistoryService.GetHistoriesByUserIdAsync(userId);
            return Ok(histories);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}