using JuegoWeb.Models.Database.Repositories.Implementations;
using JuegoWeb.Models.Database.Repositories.Interfaces;

namespace JuegoWeb.Models.Database
{
    public class UnitOfWork
    {
        private readonly JuegoWebContext _context;

        public UserRepository UserRepository { get; init; }

        public ImageRepository ImageRepository { get; init; }

        public FriendRequestRepository FriendRequestRepository { get; init; }

        public UserFriendRepository UserFriendRepository { get; init; }

        public IGameHistoryRepository IGameHistoryRepository { get; init; }

        public UnitOfWork(
            JuegoWebContext context,
            UserRepository userRepository,
             ImageRepository imageRepository,
             FriendRequestRepository friendRequestRepository,
             UserFriendRepository userFriendRepository,
             IGameHistoryRepository gameHistoryRepository)
        {
            _context = context;
            UserRepository = userRepository;
            ImageRepository = imageRepository;
            FriendRequestRepository = friendRequestRepository;
            UserFriendRepository = userFriendRepository;
            IGameHistoryRepository = gameHistoryRepository;
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}