using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Dtos;

namespace JuegoWeb.Models.Mappers;

public class FriendRequestMapper
{
    public FriendRequestDto FriendRequestToDto(FriendRequest friendRequest)
    {
        return new FriendRequestDto
        {
            Id = friendRequest.Id,
            SenderId = friendRequest.SenderId,
            SenderNickname = friendRequest.Sender.Nickname,
            ReceiverId = friendRequest.ReceiverId,
            ReceiverNickname = friendRequest.Receiver.Nickname,
            IsAccepted = friendRequest.IsAccepted
        };
    }

    public FriendRequest FriendRequestToEntity(int senderId, int receiverId)
    {
        return new FriendRequest
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            IsAccepted = false
        };
    }
}