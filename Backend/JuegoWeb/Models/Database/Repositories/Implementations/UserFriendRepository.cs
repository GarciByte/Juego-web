using JuegoWeb.Models.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace JuegoWeb.Models.Database.Repositories.Implementations;

public class UserFriendRepository : Repository<UserFriend, int>
{
    public UserFriendRepository(JuegoWebContext context) : base(context)
    {
    }

    // Agregar amigos
    public async Task AddFriendAsync(UserFriend userFriend)
    {
        await base.InsertAsync(userFriend);
    }

    // Obtener amigos de un usuario
    public async Task<List<User>> GetFriendsByUserIdAsync(int userId)
    {
        return await GetQueryable()
            .Where(uf => uf.UserId == userId)
            .Include(uf => uf.Friend)
                .ThenInclude(friend => friend.Avatar)
            .Select(uf => uf.Friend)
            .ToListAsync();
    }

    // Borrar amigos
    public async Task RemoveFriendshipAsync(int userId, int friendId)
    {
        var userFriend = await GetQueryable()
            .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.FriendId == friendId);

        var friendUser = await GetQueryable()
            .FirstOrDefaultAsync(uf => uf.UserId == friendId && uf.FriendId == userId);

        if (userFriend != null)
            _context.Set<UserFriend>().Remove(userFriend);

        if (friendUser != null)
            _context.Set<UserFriend>().Remove(friendUser);

        await _context.SaveChangesAsync();
    }
}