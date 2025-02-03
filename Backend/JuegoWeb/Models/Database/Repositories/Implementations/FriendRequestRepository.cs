using JuegoWeb.Models.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace JuegoWeb.Models.Database.Repositories.Implementations;

public class FriendRequestRepository : Repository<FriendRequest, int>
{
    public FriendRequestRepository(JuegoWebContext context) : base(context) { }

    // Obtener una solicitud de amistad por id
    public async Task<FriendRequest> GetFriendRequestByIdAsync(int id)
    {
        return await GetQueryable()
            .Include(fr => fr.Sender)
            .Include(fr => fr.Receiver)
            .FirstOrDefaultAsync(fr => fr.Id == id);
    }

    // Obtener una solicitud de amistad  por remitente y destinatario
    public async Task<FriendRequest> GetByUsersAsync(int senderId, int receiverId)
    {
        return await GetQueryable()
            .Include(fr => fr.Sender)
            .Include(fr => fr.Receiver)
            .FirstOrDefaultAsync(fr =>
                (fr.SenderId == senderId && fr.ReceiverId == receiverId) ||
                (fr.SenderId == receiverId && fr.ReceiverId == senderId));
    }

    // Crear una solicitud de amistad
    public async Task<FriendRequest> InsertFriendRequestAsync(FriendRequest friendRequest)
    {
        await base.InsertAsync(friendRequest);
        return friendRequest;
    }

    // Actualizar una solicitud de amistad
    public async Task UpdateFriendRequestAsync(FriendRequest friendRequest)
    {
        base.Update(friendRequest);
        await _context.SaveChangesAsync();
    }

    // Eliminar una solicitud de amistad
    public void DeleteFriendRequest(FriendRequest friendRequest)
    {
        base.Delete(friendRequest);
    }

    // Obtener todas las solicitudes de amistad pendientes de un usuario
    public async Task<IEnumerable<FriendRequest>> GetPendingRequestsForUserAsync(int userId)
    {
        return await GetQueryable()
            .Include(fr => fr.Sender)
            .Include(fr => fr.Receiver)
            .Where(fr => fr.ReceiverId == userId && !fr.IsAccepted)
            .ToListAsync();
    }
}