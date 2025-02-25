using System.Net.WebSockets;
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

    public WebSocketController(WebSocketNetwork websocketNetwork, UserService userService)
    {
        _websocketNetwork = websocketNetwork;
        _userService = userService;
    }

    [Authorize]
    [HttpGet]
    public async Task ConnectAsync()
    {
        // Si la petición es de tipo websocket la aceptamos
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            // Obtener id de usuario desde el Token
            UserDto user = await ReadToken();

            if (user == null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                Console.WriteLine("El usuario es null");
            }

            // Aceptamos la solicitud
            WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            // Manejamos la solicitud.
            await _websocketNetwork.HandleAsync(webSocket, user);
        }
        // En caso contrario la rechazamos
        else
        {
            Console.WriteLine("No es una conexión de WebSocket.");
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