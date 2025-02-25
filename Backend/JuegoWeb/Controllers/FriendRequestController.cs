using JuegoWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace JuegoWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FriendRequestController : ControllerBase
{
    private readonly FriendRequestService _friendRequestService;

    public FriendRequestController(FriendRequestService friendRequestService)
    {
        _friendRequestService = friendRequestService;
    }

    // Enviar una solicitud de amistad
    [HttpPost("send")]
    public async Task<IActionResult> SendFriendRequest(int senderId, int receiverId)
    {
        try
        {
            await _friendRequestService.SendFriendRequestAsync(senderId, receiverId);
            return Ok("Solicitud de amistad enviada.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Aceptar una solicitud de amistad
    [HttpPost("accept")]
    public async Task<IActionResult> AcceptFriendRequest(int requestId)
    {
        try
        {
            await _friendRequestService.AcceptFriendRequestAsync(requestId);
            return Ok("Solicitud de amistad aceptada.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Rechazar una solicitud de amistad
    [HttpPost("reject")]
    public async Task<IActionResult> RejectFriendRequest(int requestId)
    {
        try
        {
            await _friendRequestService.RejectFriendRequestAsync(requestId);
            return Ok("Solicitud de amistad rechazada.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Obtener todas las solicitudes de amistad pendientes de un usuario
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRequests(int userId)
    {
        try
        {
            var requests = await _friendRequestService.GetPendingRequestsAsync(userId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Obtener todas las solicitudes de amistad enviadas por un usuario que no han sido aceptadas
    [HttpGet("pending-sent")]
    public async Task<IActionResult> GetPendingSentRequests(int userId)
    {
        try
        {
            var requests = await _friendRequestService.GetPendingSentRequestsAsync(userId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Obtener todos los amigos de un usuario
    [HttpGet("friends")]
    public async Task<IActionResult> GetFriends(int userId)
    {
        try
        {
            var friends = await _friendRequestService.GetFriendsAsync(userId);
            return Ok(friends);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Borrar un amigo
    [HttpDelete("remove-friend/{userId}/{friendId}")]
    public async Task<IActionResult> RemoveFriend(int userId, int friendId)
    {
        try
        {
            await _friendRequestService.RemoveFriendAsync(userId, friendId);
            return Ok("Amigo eliminado con éxito.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}