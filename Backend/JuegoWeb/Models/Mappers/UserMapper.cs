using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Dtos;

namespace JuegoWeb.Models.Mappers;

public class UserMapper
{
    private readonly ImageMapper _imageMapper;

    public UserMapper(ImageMapper imageMapper)
    {
        _imageMapper = imageMapper;
    }

    public UserDto UserToDto(User user, HttpRequest request = null)
    {
        return new UserDto
        {
            UserId = user.Id,
            Nickname = user.Nickname,
            Email = user.Email,
            Avatar = user.Avatar != null ? _imageMapper.ToDto(user.Avatar, request) : null,
            Role = user.Role,
            IsBanned = user.IsBanned,
            Status = UserStatus.Offline,
        };
    }

    public UserProfileDto UserProfileToDto(User user, HttpRequest request = null)
    {
        return new UserProfileDto
        {
            UserId = user.Id,
            Nickname = user.Nickname,
            Email = user.Email,
            Avatar = user.Avatar != null ? _imageMapper.ToDto(user.Avatar, request) : null,
            Role = user.Role,
            IsBanned = user.IsBanned,
            Password = user.Password,
            Status = UserStatus.Offline,
        };
    }

    public IEnumerable<UserDto> UsersToDto(IEnumerable<User> users, HttpRequest request = null)
    {
        return users.Select(user => UserToDto(user, request));
    }
}