using JuegoWeb.Helpers;
using JuegoWeb.Models.Database.Entities;

namespace JuegoWeb.Models.Database;

public class Seeder
{
    private readonly JuegoWebContext _context;

    public Seeder(JuegoWebContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await SeedUsersAsync();

        await _context.SaveChangesAsync();
    }

    private async Task SeedUsersAsync()
    {
        var image1 = new Image { Name = "imagen1", Path = "avatars/imagen1.jpg" };
        var image2 = new Image { Name = "imagen2", Path = "avatars/imagen2.jpg" };

        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        User[] users = [
        new User {
            Nickname = "David",
            Email = "david@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "Admin",
            AvatarId = image1.Id
        },
        new User {
            Nickname = "Usuario",
            Email = "usuario@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            AvatarId = image2.Id
        }
    ];

        await _context.User.AddRangeAsync(users);
        await _context.SaveChangesAsync();
    }

}
