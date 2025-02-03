using JuegoWeb.Models.Mappers;
using JuegoWeb.Services;
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
    }
}