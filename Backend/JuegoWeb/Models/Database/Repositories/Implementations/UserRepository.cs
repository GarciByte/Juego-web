using JuegoWeb.Models.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace JuegoWeb.Models.Database.Repositories.Implementations;

public class UserRepository : Repository<User, int>
{
    public UserRepository(JuegoWebContext context) : base(context) { }

    // Obtener usuario por nickname
    public async Task<User> GetUserByNickname(string nickname)
    {
        return await GetQueryable()
            .Include(user => user.Avatar)
            .FirstOrDefaultAsync(user => user.Nickname.ToLower() == nickname.ToLower());
    }

    // Obtener usuario por email
    public async Task<User> GetUserByEmail(string email)
    {
        return await GetQueryable()
            .Include(user => user.Avatar)
            .FirstOrDefaultAsync(user => user.Email == email);
    }

    // Obtener usuario por id
    public async Task<User> GetUserById(int id)
    {
        return await GetQueryable()
            .Include(user => user.Avatar)
            .FirstOrDefaultAsync(user => user.Id == id);
    }

    // Crear un nuevo usuario
    public async Task<User> InsertUserAsync(User newUser)
    {
        await base.InsertAsync(newUser);
        return newUser;
    }

    // Obtener todos los usuarios
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await GetQueryable()
            .Include(user => user.Avatar)
            .ToListAsync();
    }
}