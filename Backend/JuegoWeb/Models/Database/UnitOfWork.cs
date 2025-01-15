using JuegoWeb.Models.Database.Repositories.Implementations;

namespace JuegoWeb.Models.Database
{
    public class UnitOfWork
    {
        private readonly JuegoWebContext _context;

        public UserRepository UserRepository { get; init; }
        public ImageRepository ImageRepository { get; init; }

        public UnitOfWork(
            JuegoWebContext context,
            UserRepository userRepository,
             ImageRepository imageRepository
            )
        {
            _context = context;

            UserRepository = userRepository;
            ImageRepository = imageRepository;
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
