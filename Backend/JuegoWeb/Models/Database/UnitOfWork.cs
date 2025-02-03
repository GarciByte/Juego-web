using JuegoWeb.Models.Database.Repositories.Implementations;

namespace JuegoWeb.Models.Database
{
    public class UnitOfWork
    {
        private readonly JuegoWebContext _context;

        public UserRepository UserRepository { get; init; }

        public ImageRepository ImageRepository { get; init; }

        public FriendRequestRepository FriendRequestRepository { get; init; }

        public UserFriendRepository UserFriendRepository { get; init; }

        public UnitOfWork(
            JuegoWebContext context,
            UserRepository userRepository,
             ImageRepository imageRepository,
             FriendRequestRepository friendRequestRepository,
             UserFriendRepository userFriendRepository)
        {
            _context = context;
            UserRepository = userRepository;
            ImageRepository = imageRepository;
            FriendRequestRepository = friendRequestRepository;
            UserFriendRepository = userFriendRepository;
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}