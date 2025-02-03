using JuegoWeb.Helpers;
using JuegoWeb.Models.Database.Entities;
using Microsoft.EntityFrameworkCore;

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
        // Crear imágenes
        var image1 = new Image { Name = "imagen1", Path = "avatars/imagen1.jpg" };
        var image2 = new Image { Name = "imagen2", Path = "avatars/imagen2.jpg" };
        var image3 = new Image { Name = "imagen3" + "_default", Path = "avatars/avatar.png" };

        await _context.Images.AddRangeAsync(image1, image2, image3);
        await _context.SaveChangesAsync();

        // Crear usuarios
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
        },
        new User {
            Nickname = "Samuel",
            Email = "samuel@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            AvatarId = image3.Id
        }
    ];

        await _context.User.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Obtener los usuarios
        var david = await _context.User.FirstOrDefaultAsync(u => u.Nickname == "David");
        var usuario = await _context.User.FirstOrDefaultAsync(u => u.Nickname == "Usuario");

        // Crear la relación de amistad de ambos
        var userFriendDavid = new UserFriend
        {
            UserId = david.Id,
            FriendId = usuario.Id
        };

        var userFriendUsuario = new UserFriend
        {
            UserId = usuario.Id,
            FriendId = david.Id
        };

        await _context.UserFriends.AddRangeAsync(userFriendDavid, userFriendUsuario);

        // Crear la solicitud de amistad aceptada
        var friendRequest = new FriendRequest
        {
            SenderId = david.Id,
            ReceiverId = usuario.Id,
            IsAccepted = true
        };

        await _context.FriendRequest.AddAsync(friendRequest);
        await _context.SaveChangesAsync();
    }
}