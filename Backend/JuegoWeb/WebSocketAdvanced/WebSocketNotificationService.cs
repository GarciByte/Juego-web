using JuegoWeb.Models.Dtos;

namespace JuegoWeb.WebSocketAdvanced;

public class WebSocketNotificationService
{
    // Notificar una actualización de la lista de amigos (por añadir o borrar)
    public async Task NotifyFriendListUpdatedAsync(int userId, List<UserDto> updatedFriends, IWebSocketMessageSender sender)
    {
        var message = new WebSocketMessage
        {
            Type = MsgType.FriendListUpdate,
            Id = userId,
            Content = updatedFriends
        };

        await sender.SendToUserAsync(userId, message);
    }

    // Notificar una actualización del estado del usuario a su amigo (conectado, desconectado o jugando)
    public async Task NotifyUserStatusToFriendAsync(int friendId, int userId, UserStatus status, IWebSocketMessageSender sender)
    {
        var message = new WebSocketMessage
        {
            Type = MsgType.FriendStatusUpdate,
            Id = friendId,
            Content = new { UserId = userId, Status = status.ToString() }
        };

        await sender.SendToUserAsync(friendId, message);
    }

    // Notificar una actualización de las solicitudes de amistad (alguien te envía una)
    public async Task NotifyFriendRequestUpdatedAsync(int userId, FriendRequestDto friendRequest, IWebSocketMessageSender sender)
    {
        var message = new WebSocketMessage
        {
            Type = MsgType.FriendRequestUpdate,
            Id = userId,
            Content = friendRequest
        };
        Console.WriteLine($"Solicitud de amistad de {friendRequest.SenderNickname} actualizada, se le notificará a {friendRequest.ReceiverNickname}");
        await sender.SendToUserAsync(userId, message);
    }

    // Notificar de una invitación a una partida
    public async Task NotifyGameInvitationAsync(int userId, GameInvitationDto invitation, IWebSocketMessageSender sender)
    {
        var message = new WebSocketMessage
        {
            Type = MsgType.GameInvitation,
            Id = userId,
            Content = invitation
        };

        await sender.SendToUserAsync(userId, message);
    }

    // Notificar una actualización en una sala de juego activa (alguien se desconecta)
    public async Task NotifyRoomUpdatedAsync(int userId, GameRoomDto room, IWebSocketMessageSender sender)
    {
        var message = new WebSocketMessage
        {
            Type = MsgType.GameRoom,
            Id = userId,
            Content = room
        };

        await sender.SendToUserAsync(userId, message);
    }

    // Notificar estadísticas globales: cantidad total de jugadores, partidas en curso y jugadores en partidas
    public async Task NotifyStatsUpdatedAsync(int totalPlayers, int activeGames, int playersInGames, IWebSocketMessageSender sender)
    {
        var stats = new GameStatsDto
        {
            TotalPlayers = totalPlayers,
            ActiveGames = activeGames,
            PlayersInGames = playersInGames
        };

        var message = new WebSocketMessage
        {
            Type = MsgType.StatsUpdate,
            Content = stats
        };

        await sender.SendToAllAsync(message);
    }
}