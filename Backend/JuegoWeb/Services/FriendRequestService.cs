using JuegoWeb.Models.Database;
using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Dtos;
using JuegoWeb.Models.Mappers;
using JuegoWeb.WebSocketAdvanced;

namespace JuegoWeb.Services;

public class FriendRequestService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly UserMapper _userMapper;
    private readonly FriendRequestMapper _friendRequestMapper;
    private readonly WebSocketNotificationService _webSocketNotificationService;
    private readonly IWebSocketMessageSender _webSocketMessageSender;

    public FriendRequestService(
        UnitOfWork unitOfWork, 
        UserMapper userMapper, 
        FriendRequestMapper friendRequestMapper, 
        WebSocketNotificationService webSocketNotificationService,
        IWebSocketMessageSender webSocketMessageSender)
    {
        _unitOfWork = unitOfWork;
        _userMapper = userMapper;
        _friendRequestMapper = friendRequestMapper;
        _webSocketNotificationService = webSocketNotificationService;
        _webSocketMessageSender = webSocketMessageSender;
    }

    // Enviar una solicitud de amistad
    public async Task SendFriendRequestAsync(int senderId, int receiverId)
    {
        if (senderId == receiverId)
            throw new Exception("No puedes enviarte una solicitud de amistad a ti mismo.");

        var existingRequest = await _unitOfWork.FriendRequestRepository.GetByUsersAsync(senderId, receiverId);
        var existingRequestReverse = await _unitOfWork.FriendRequestRepository.GetByUsersAsync(receiverId, senderId);

        if (existingRequest != null || existingRequestReverse != null)
        {
            if (existingRequest.IsAccepted || existingRequestReverse.IsAccepted)
            {
                throw new Exception("La solicitud ya fue aceptada.");
            }
            else 
            {
                throw new Exception("Ya existe una solicitud de amistad entre estos usuarios.");
            }
        }

        var friendRequest = _friendRequestMapper.FriendRequestToEntity(senderId, receiverId);
        await _unitOfWork.FriendRequestRepository.InsertFriendRequestAsync(friendRequest);
        await _unitOfWork.SaveAsync();

        // Notificar una actualización
        await _webSocketNotificationService.NotifyFriendRequestUpdatedAsync(receiverId, _webSocketMessageSender);
    }

    // Aceptar una solicitud de amistad
    public async Task AcceptFriendRequestAsync(int requestId)
    {
        var friendRequest = await _unitOfWork.FriendRequestRepository.GetFriendRequestByIdAsync(requestId);
        if (friendRequest == null)
            throw new Exception("Solicitud de amistad no encontrada.");

        if (friendRequest.IsAccepted)
            throw new Exception("La solicitud ya fue aceptada.");

        friendRequest.IsAccepted = true;
        await _unitOfWork.FriendRequestRepository.UpdateFriendRequestAsync(friendRequest);

        var userFriend = new UserFriend
        {
            UserId = friendRequest.SenderId,
            FriendId = friendRequest.ReceiverId
        };

        var reverseUserFriend = new UserFriend
        {
            UserId = friendRequest.ReceiverId,
            FriendId = friendRequest.SenderId
        };

        await _unitOfWork.UserFriendRepository.AddFriendAsync(userFriend);
        await _unitOfWork.UserFriendRepository.AddFriendAsync(reverseUserFriend);

        await _unitOfWork.SaveAsync();

        // Notificar una actualización
        await _webSocketNotificationService.NotifyFriendListUpdatedAsync(friendRequest.SenderId, _webSocketMessageSender);
    }

    // Rechazar una solicitud de amistad
    public async Task RejectFriendRequestAsync(int requestId)
    {
        var friendRequest = await _unitOfWork.FriendRequestRepository.GetFriendRequestByIdAsync(requestId);
        if (friendRequest == null)
            throw new Exception("Solicitud de amistad no encontrada.");

        if (friendRequest.IsAccepted)
            throw new Exception("La solicitud ya fue aceptada.");

        _unitOfWork.FriendRequestRepository.DeleteFriendRequest(friendRequest);
        await _unitOfWork.SaveAsync();
    }

    // Obtener todas las solicitudes de amistad pendientes de un usuario
    public async Task<List<FriendRequestDto>> GetPendingRequestsAsync(int userId)
    {
        var requests = await _unitOfWork.FriendRequestRepository.GetPendingRequestsForUserAsync(userId);
        return requests.Select(_friendRequestMapper.FriendRequestToDto).ToList();
    }

    // Obtener todas las solicitudes de amistad enviadas por un usuario que no han sido aceptadas
    public async Task<List<FriendRequestDto>> GetPendingSentRequestsAsync(int userId)
    {
        var requests = await _unitOfWork.FriendRequestRepository.GetPendingSentRequestsForUserAsync(userId);
        return requests.Select(_friendRequestMapper.FriendRequestToDto).ToList();
    }

    // Obtener todos los amigos de un usuario
    public async Task<List<UserDto>> GetFriendsAsync(int userId)
    {
        var userFriends = await _unitOfWork.UserFriendRepository.GetFriendsByUserIdAsync(userId);
        if (userFriends == null)
            return new List<UserDto>();

        return _userMapper.UsersToDto(userFriends, null).ToList();
    }

    // Borrar un amigo
    public async Task RemoveFriendAsync(int userId, int friendId)
    {
        await _unitOfWork.UserFriendRepository.RemoveFriendshipAsync(userId, friendId);

        // Eliminar la solicitud de amistad aceptada
        var friendRequest = await _unitOfWork.FriendRequestRepository.GetByUsersAsync(userId, friendId);

        if (friendRequest != null && friendRequest.IsAccepted)
        {
            _unitOfWork.FriendRequestRepository.DeleteFriendRequest(friendRequest);
        }

        await _unitOfWork.SaveAsync();

        // Notificar una actualización
        await _webSocketNotificationService.NotifyFriendListUpdatedAsync(friendId, _webSocketMessageSender);
    }
}