using JuegoWeb.Models.Database.Entities;
namespace JuegoWeb.Models.Database.Repositories.Implementations;

public class ImageRepository : Repository<Image, int>
{
    public ImageRepository(JuegoWebContext context) : base(context)
    {
    }
}
