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
            IsBanned = false,
            Avatar = avatar
        };

        await _unitOfWork.UserRepository.InsertUserAsync(newUser);
        await _unitOfWork.SaveAsync();

        return newUser;
    }

    // Modificar los datos del usuario
    public async Task ModifyUserAsync(ModifyUserDto modifyUserDto)
    {
        // Obtener usuario
        var existingUser = await _unitOfWork.UserRepository.GetUserById(modifyUserDto.UserId);
        if (existingUser == null)
        {
            throw new InvalidOperationException("Usuario no encontrado");
        }

        // Actualizar los datos
        if (!string.IsNullOrEmpty(modifyUserDto.Nickname) && existingUser.Nickname != modifyUserDto.Nickname)
        {
            // Verifica si ya existe un usuario con el mismo nickname
            var existingUserNickname = await GetUserByNicknameAsync(modifyUserDto.Nickname);
            if (existingUserNickname != null)
            {
                throw new InvalidOperationException("El nickname ya está en uso");
            }

            existingUser.Nickname = modifyUserDto.Nickname;
        }

        if (!string.IsNullOrEmpty(modifyUserDto.Email) && existingUser.Email != modifyUserDto.Email)
        {
            // Verifica si ya existe un usuario con el mismo correo
            var existingUserEmail = await GetUserByEmailAsync(modifyUserDto.Email);
            if (existingUserEmail != null)
            {
                throw new InvalidOperationException("El correo electrónico ya está en uso");
            }

            existingUser.Email = modifyUserDto.Email;
        }

        // Avatar
        if (modifyUserDto.RemoveAvatar)
        {
            // Avatar por defecto
            existingUser.Avatar = new Image { Name = $"{existingUser.Nickname}_default", Path = "avatars/avatar.png" };
        }
        else if (modifyUserDto.AvatarFile != null)
        {
            // Nuevo avatar
            var newAvatar = await _imageService.InsertImageAsync(new CreateUpdateImageRequest
            {
                Name = $"{existingUser.Nickname}_avatar",
                File = modifyUserDto.AvatarFile
            });

            if (newAvatar != null)
            {
                existingUser.Avatar = newAvatar;
            }
        }

        await UpdateUser(existingUser);
        //Console.WriteLine("Usuario actualizado correctamente.", existingUser);
        await _unitOfWork.SaveAsync();
    }

    // Modificar el rol del usuario
    public async Task ModifyUserRoleAsync(int userId, string newRole)
    {
        // Obtener usuario
        var existingUser = await _unitOfWork.UserRepository.GetUserById(userId);
        if (existingUser == null)
        {
            throw new InvalidOperationException("Usuario no encontrado");
        }

        // Actualizar el rol
        if (!string.IsNullOrEmpty(newRole))
        {
            existingUser.Role = newRole;
        }

        await UpdateUser(existingUser);
        await _unitOfWork.SaveAsync();
    }

    // Modificar la prohibición de un usuario
    public async Task ModifyUserBanAsync(int userId, bool isBanned)
    {
        // Obtener usuario
        var existingUser = await _unitOfWork.UserRepository.GetUserById(userId);
        if (existingUser == null)
        {
            throw new InvalidOperationException("Usuario no encontrado");
        }

        // Actualizar la prohibición
        if (isBanned)
        {
            existingUser.IsBanned = true;
        }
        else
        {
            existingUser.IsBanned = false;
        }

        //Console.WriteLine($"Estado de la prohibición del usuario {existingUser.Nickname}: {isBanned}");

        await UpdateUser(existingUser);
        await _unitOfWork.SaveAsync();
    }

    // Modificar contraseña del usuario
    public async Task ModifyPasswordAsync(int userId, string newPassword)
    {
        // Obtener usuario
        var existingUser = await _unitOfWork.UserRepository.GetUserById(userId);
        if (existingUser == null)
        {
            throw new InvalidOperationException("Usuario no encontrado");
        }

        if (!string.IsNullOrEmpty(newPassword) && existingUser.Password != PasswordHelper.Hash(newPassword))
        {
            existingUser.Password = PasswordHelper.Hash(newPassword);
        }
        else
        {
            throw new InvalidOperationException("La contraseña es nula o similar a la anterior");
        }

        await UpdateUser(existingUser);
        //Console.WriteLine("Usuario actualizado correctamente.", existingUser);
        await _unitOfWork.SaveAsync();
    }

    // Actualizar los datos del usuario
    public async Task UpdateUser(User user)
    {
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveAsync();
    }
}