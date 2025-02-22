using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Dtos;
using JuegoWeb.Models.Mappers;
using JuegoWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JuegoWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly UserMapper _userMapper;

        public UserController(UserService userService, UserMapper userMapper)
        {
            _userService = userService;
            _userMapper = userMapper;
        }

        // Obtener un usuario por su id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var user = await _userService.GetUserByIdAsync(id, HttpContext.Request);

            if (user == null)
            {
                return NotFound(new { message = $"El usuario con el id '{id}' no ha sido encontrado." });
            }

            return Ok(user);
        }

        // Obtener usuarios por nickname
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsersByNicknameAsync(string nickname)
        {
            // Quitar tildes y convertir a minúsculas
            string normalizedNickname = Normalize(nickname);

            var allUsers = await _userService.GetAllUsersAsync(HttpContext.Request);

            // Filtrar usuarios
            var filteredUsers = allUsers
                .Where(user => Normalize(user.Nickname).Contains(normalizedNickname))
                .ToList();

            return Ok(filteredUsers);
        }

        // Obtener todos los usuarios
        [HttpGet("allUsers")]
        public async Task<IActionResult> GetAllUsersAsync()
        {
            var users = await _userService.GetAllUsersAsync(HttpContext.Request);
            return Ok(users);
        }

        [Authorize]
        [HttpPut("modifyUser")]
        public async Task<IActionResult> ModifyUser([FromBody] UserProfileDto userDto)
        {

            // Obtener datos del usuario para modificarse a si mismo
            UserProfileDto userData = await ReadToken();

            if (userData == null)
            {
                Console.WriteLine("Token inválido o usuario no encontrado.");
                return BadRequest("El usuario es null");
            }

            Console.WriteLine($"Usuario autenticado: ID = {userData.UserId}, Email = {userData.Email}");
            userDto.UserId = userData.UserId;
            try
            {
                await _userService.ModifyUserAsync(userDto);
                return Ok("Usuario actualizado correctamente.");
            }
            catch (InvalidOperationException)
            {
                return BadRequest("No pudo modificarse el usuario.");
            }
        }

        // Solo pueden usar este método los usuarios cuyo rol sea admin
        [Authorize(Roles = "Admin")]
        [HttpPut("modifyUserRole")]
        public async Task<IActionResult> ModifyUserRole(ModifyRoleRequest request)
        {

            // Obtener datos del usuario
            UserDto userData = await _userService.GetUserByIdAsync(request.UserId);

            if (userData == null)
            {
                return BadRequest("El usuario es null");
            }

            try
            {
                if (request.NewRole == "User" || request.NewRole == "Admin")
                {
                    await _userService.ModifyUserRoleAsync(request.UserId, request.NewRole);
                    return Ok("Rol de usuario actualizado correctamente.");
                }
                else
                {
                    return BadRequest("El nuevo rol debe ser User o Admin");
                }

            }
            catch (InvalidOperationException)
            {
                return BadRequest("No pudo modificarse el rol del usuario.");
            }
        }

        [Authorize]
        [HttpPut("modifyPassword")]
        public async Task<IActionResult> ModifyPassword([FromBody] NewPasswordDto newPasswordRequest)
        {

            if (newPasswordRequest == null)
            {
                return BadRequest("La nueva contraseña es nula.");
            }

            // Obtener datos del usuario para modificarse a si mismo
            UserProfileDto userData = await ReadToken();

            if (userData == null)
            {
                Console.WriteLine("Token inválido o usuario no encontrado.");
                return BadRequest("El usuario es null");
            }

            Console.WriteLine($"Usuario autenticado: ID = {userData.UserId}, Email = {userData.Email}");

            try
            {
                await _userService.ModifyPasswordAsync(userData.UserId, newPasswordRequest.newPassword);
                return Ok("Contraseña actualizada correctamente.");
            }
            catch (InvalidOperationException)
            {
                return BadRequest("No pudo modificarse la contraseña.");
            }
        }

        /*
        // Elimina un usuario
        [Authorize(Roles = "Admin")]
        [HttpDelete("deleteUser/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                //await _userService.DeleteUserAsync(userId);

                return Ok("Usuario eliminado correctamente.");
            }
            catch (InvalidOperationException)
            {
                return BadRequest("No pudo eliminarse el usuario");
            }
        }*/

        // Método para quitar tildes y convertir a minúsculas
        private static string Normalize(string input)
        {
            return input
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ü", "u")
                .Replace("ñ", "n").Replace("Á", "A").Replace("É", "E")
                .Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U")
                .Replace("Ü", "U").Replace("Ñ", "N")
                .ToLower();
        }

        // Leer datos del token
        private async Task<UserProfileDto> ReadToken()
        {
            try
            {
                string id = User.Claims.FirstOrDefault().Value;
                User user = await _userService.GetUserByIdAsyncNoDto(Int32.Parse(id));
                UserProfileDto userDto = _userMapper.UserProfileToDto(user);
                return userDto;
            }
            catch (Exception)
            {
                Console.WriteLine("La ID del usuario es null");
                return null;
            }
        }
    }
}