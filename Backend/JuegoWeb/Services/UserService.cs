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

    public async Task<UserDto> GetUserByIdAsync(int id, HttpRequest request = null)
    {
        var user = await _unitOfWork.UserRepository.GetUserById(id);

        if (user == null)
        {
            return null;
        }

        return _userMapper.UserToDto(user, request);
    }

    public async Task<User> GetUserByIdAsyncNoDto(int id)
    {
        var user = await _unitOfWork.UserRepository.GetUserById(id);

        if (user == null)
        {
            return null;
        }

        return user;
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

        // Imagen de avatar por defecto si no se seleccionó ninguno
        if (avatar == null)
        {
            avatar = new Image { Name = model.Nickname + "_avatar" + "_default", Path = "avatars/avatar.png" };
        }

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

    // Modificar los datos del usuario
    public async Task ModifyUserAsync(UserProfileDto userDto)
    {
        var existingUser = await _unitOfWork.UserRepository.GetUserById(userDto.UserId);

        if (existingUser != null)
        {
            Console.WriteLine("El usuario con ID ", userDto.UserId, " no existe.");
        }

        Console.WriteLine("ID del usuario: " + existingUser.Id);

        if (!string.IsNullOrEmpty(userDto.Nickname) && existingUser.Nickname != userDto.Nickname)
        {
            existingUser.Nickname = userDto.Nickname;
        }

        if (!string.IsNullOrEmpty(userDto.Email) && existingUser.Email != userDto.Email)
        {
            existingUser.Email = userDto.Email;
        }

        await UpdateUser(existingUser);
        Console.WriteLine("Usuario actualizado correctamente.", existingUser);
        await _unitOfWork.SaveAsync();
    }

    // Modificar el rol del usuario
    public async Task ModifyUserRoleAsync(int userId, string newRole)
    {
        var existingUser = await _unitOfWork.UserRepository.GetUserById(userId);


        if (existingUser == null)
        {
            throw new InvalidOperationException("Usuario con ID:" + userId + "no encontrado.");
        }

        Console.WriteLine("ID del usuario: " + existingUser.Id);

        if (!string.IsNullOrEmpty(newRole))
        {
            existingUser.Role = newRole;
        }

        await UpdateUser(existingUser);
        await _unitOfWork.SaveAsync();
    }

    // Modificar contraseña del usuario
    public async Task ModifyPasswordAsync(int userId, string newPassword)
    {
        var existingUser = await _unitOfWork.UserRepository.GetUserById(userId);

        if (existingUser != null)
        {
            Console.WriteLine("El usuario con ID ", userId, " no existe.");
        }

        if (!string.IsNullOrEmpty(newPassword) && existingUser.Password != PasswordHelper.Hash(newPassword))
        {
            existingUser.Password = PasswordHelper.Hash(newPassword);
        }
        else
        {
            Console.WriteLine("La contraseña es nula o similar a la anterior");
        }

        await UpdateUser(existingUser);
        Console.WriteLine("Usuario actualizado correctamente.", existingUser);
        await _unitOfWork.SaveAsync();
    }

    public async Task UpdateUser(User user)
    {
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveAsync();
    }
}