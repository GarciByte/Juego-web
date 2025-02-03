using System.Net.WebSockets;
using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Dtos;
using JuegoWeb.Services;
using JuegoWeb.WebSocketAdvanced;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JuegoWeb.Controllers;

[Route("socket")]
[ApiController]
public class WebSocketController : ControllerBase
{
    private readonly WebSocketNetwork _websocketNetwork;
    private readonly UserService _userService;
    private readonly FriendRequestService _friendRequestService;

    public WebSocketController(WebSocketNetwork websocketNetwork, UserService userService, FriendRequestService friendRequestService)
    {
        _websocketNetwork = websocketNetwork;
        _userService = userService;
        _friendRequestService = friendRequestService;
    }

    [Authorize]
    [HttpGet]
    public async Task ConnectAsync()
    {
        // Si la petición es de tipo websocket la aceptamos
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            // Aceptamos la solicitud
            WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            // Obtener id de usuario desde el Token
            UserDto user = await ReadToken();

            if (user == null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest; ;
            }

            // Obtener los amigos del usuario
            var friends = await _friendRequestService.GetFriendsAsync(user.UserId);

            // Manejamos la solicitud.
            await _websocketNetwork.HandleAsync(webSocket, user, friends);
        }
        // En caso contrario la rechazamos
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        // Cuando este método finalice, se cerrará automáticamente la conexión con el websocket
    }

    // Leer datos del token
    private async Task<UserDto> ReadToken()
    {
        try
        {
            string id = User.Claims.FirstOrDefault().Value;
            UserDto user = await _userService.GetUserByIdAsync(Int32.Parse(id, null));
            return user;
        }
        catch (Exception)
        {
            Console.WriteLine("La ID del usuario es null");
            return null;
        }
    }
}
