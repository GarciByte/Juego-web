using JuegoWeb.Helpers;
using JuegoWeb.Models.Database;
using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Dtos;
using JuegoWeb.Models.Mappers;


namespace JuegoWeb.Services;

public class UserService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly UserMapper _userMapper;
    private readonly ImageService _imageService;

    public UserService(UnitOfWork unitOfWork, UserMapper userMapper, ImageService imageService)
    {
        _unitOfWork = unitOfWork;
        _userMapper = userMapper;
        _imageService = imageService;
    }

    public async Task<List<UserDto>> GetAllUsersAsync(HttpRequest request)
    {
        var users = await _unitOfWork.UserRepository.GetAllUsersAsync();
        return _userMapper.UsersToDto(users, request).ToList();
    }

    public async Task<UserDto> GetUserByNicknameAsync(string nickname, HttpRequest request = null)
    {
        var user = await _unitOfWork.UserRepository.GetUserByNickname(nickname);
        
        if (user == null)
        {
            return null;
        }

        return _userMapper.UserToDto(user, request);
    }

    public async Task<UserDto> GetUserByEmailAsync(string email, HttpRequest request = null)
    {
        var user = await _unitOfWork.UserRepository.GetUserByEmail(email);
        if (user == null)
        {
            return null;
        }
        return _userMapper.UserToDto(user, request);
    }

    public async Task<UserDto> GetUserByIdAsync(int id, HttpRequest request)
    {
        var user = await _unitOfWork.UserRepository.GetUserById(id);

        if (user == null)
        {
            return null;
        }

        return _userMapper.UserToDto(user, request);
    }

    public async Task<User> LoginAsync(string email, string nickname, string password)
    {
        User user = null;

        // Buscar usuario por email si se inicia sesión con el email
        if (!string.IsNullOrEmpty(email))
        {
            user = await _unitOfWork.UserRepository.GetUserByEmail(email);
        }

        // Buscar usuario por nickname si se inicia sesión con el nickname
        if (user == null && !string.IsNullOrEmpty(nickname))
        {
            user = await _unitOfWork.UserRepository.GetUserByNickname(nickname);
        }

        if (user == null || user.Password != PasswordHelper.Hash(password))
        {
            return null;
        }

        return user;
    }

    public async Task<User> RegisterAsync(RegisterDto model)
    {
        // Verifica si el usuario ya existe
        var existingUser = await GetUserByEmailAsync(model.Email, null);
        if (existingUser != null)
        {
            throw new Exception("El usuario ya existe.");
        }

        // Guarda la imagen de avatar
        var avatar = await _imageService.InsertImageAsync(new CreateUpdateImageRequest
        {
            Name = model.Nickname + "_avatar",
            File = model.Avatar
        });

        var newUser = new User
        {
            Nickname = model.Nickname,
            Email = model.Email,
            Password = PasswordHelper.Hash(model.Password),
            Role = "User", // Rol por defecto
            Avatar = avatar
        };

        await _unitOfWork.UserRepository.InsertUserAsync(newUser);
        await _unitOfWork.SaveAsync();

        return newUser;
    }
}
