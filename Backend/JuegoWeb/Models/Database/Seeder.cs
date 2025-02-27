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
        // Crear imágenes de los usuarios
        var image1 = new Image { Name = "imagen1", Path = "avatars/imagen1.jpg" };
        var image2 = new Image { Name = "imagen2", Path = "avatars/imagen2.jpg" };
        var image3 = new Image { Name = "imagen3" + "_default", Path = "avatars/avatar.png" };
        var image4 = new Image { Name = "imagen4" + "_default", Path = "avatars/avatar.png" };
        var image5 = new Image { Name = "imagen5" + "_default", Path = "avatars/avatar.png" };
        var image6 = new Image { Name = "imagen6" + "_default", Path = "avatars/avatar.png" };
        var image7 = new Image { Name = "imagen7" + "_default", Path = "avatars/avatar.png" };
        var image8 = new Image { Name = "imagen8" + "_default", Path = "avatars/avatar.png" };
        var image9 = new Image { Name = "imagen9" + "_default", Path = "avatars/avatar.png" };
        var image10 = new Image { Name = "imagen10" + "_default", Path = "avatars/avatar.png" };
        var image11 = new Image { Name = "imagen11" + "_default", Path = "avatars/avatar.png" };

        await _context.Images.AddRangeAsync(
            image1, 
            image2, 
            image3, 
            image4, 
            image5, 
            image6, 
            image7, 
            image8, 
            image9,
            image10,
            image11
            );

        await _context.SaveChangesAsync();

        // Crear usuarios
        User[] users = [
        new User {
            Nickname = "David",
            Email = "david@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "Admin",
            IsBanned = false,
            AvatarId = image1.Id
        },
        new User {
            Nickname = "Usuario",
            Email = "usuario@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            IsBanned = false,
            AvatarId = image2.Id
        },
        new User {
            Nickname = "Samuel",
            Email = "samuel@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            IsBanned = false,
            AvatarId = image3.Id
        },
        new User {
            Nickname = "Manolo",
            Email = "manolo@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            IsBanned = false,
            AvatarId = image4.Id
        },
        new User {
            Nickname = "Pedro",
            Email = "pedro@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            IsBanned = false,
            AvatarId = image5.Id
        },
        new User {
            Nickname = "Daniel",
            Email = "daniel@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            IsBanned = false,
            AvatarId = image6.Id
        },
        new User {
            Nickname = "Alicia",
            Email = "alicia@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            IsBanned = false,
            AvatarId = image7.Id
        },
        new User {
            Nickname = "Antonia",
            Email = "antonia@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            IsBanned = false,
            AvatarId = image8.Id
        },
        new User {
            Nickname = "Juan",
            Email = "juan@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            IsBanned = false,
            AvatarId = image9.Id
        },
        new User {
            Nickname = "Roberto",
            Email = "roberto@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            IsBanned = false,
            AvatarId = image10.Id
        },
        new User {
            Nickname = "Eva",
            Email = "eva@gmail.com",
            Password = PasswordHelper.Hash("123456"),
            Role = "User",
            IsBanned = false,
            AvatarId = image11.Id
        },
    ];

        await _context.User.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Obtener los usuarios 
        var david = await _context.User.FirstOrDefaultAsync(u => u.Nickname == "David");
        var usuario = await _context.User.FirstOrDefaultAsync(u => u.Nickname == "Usuario");
        var samuel = await _context.User.FirstOrDefaultAsync(u => u.Nickname == "Samuel");
        var manolo = await _context.User.FirstOrDefaultAsync(u => u.Nickname == "Manolo");
        var daniel = await _context.User.FirstOrDefaultAsync(u => u.Nickname == "Daniel");
        var pedro = await _context.User.FirstOrDefaultAsync(u => u.Nickname == "Pedro");

        // Crear la relación de amistad con David

        var userFriendDavid_Usuario = new UserFriend // Usuario
        {
            UserId = david.Id,
            FriendId = usuario.Id
        };

        var userFriendUsuario = new UserFriend
        {
            UserId = usuario.Id,
            FriendId = david.Id
        };

        var userFriendDavid_Samuel = new UserFriend // Samuel
        {
            UserId = david.Id,
            FriendId = samuel.Id
        };

        var userFriendSamuel = new UserFriend
        {
            UserId = samuel.Id,
            FriendId = david.Id
        };

        var userFriendDavid_Manolo = new UserFriend // Manolo
        {
            UserId = david.Id,
            FriendId = manolo.Id
        };

        var userFriendManolo = new UserFriend
        {
            UserId = manolo.Id,
            FriendId = david.Id
        };

        var userFriendDavid_Pedro = new UserFriend // Pedro
        {
            UserId = david.Id,
            FriendId = pedro.Id
        };

        var userFriendPedro = new UserFriend
        {
            UserId = pedro.Id,
            FriendId = david.Id
        };

        var userFriendDavid_Daniel = new UserFriend // Daniel
        {
            UserId = david.Id,
            FriendId = daniel.Id
        };

        var userFriendDaniel = new UserFriend
        {
            UserId = daniel.Id,
            FriendId = david.Id
        };

        await _context.UserFriends.AddRangeAsync(
            userFriendDavid_Usuario,
            userFriendUsuario,
            userFriendDavid_Samuel,
            userFriendSamuel,
            userFriendDavid_Manolo,
            userFriendManolo,
            userFriendDavid_Pedro,
            userFriendPedro,
            userFriendDavid_Daniel,
            userFriendDaniel
            );

        // Crear las solicitudes de amistad aceptadas
        var friendRequest_1 = new FriendRequest
        {
            SenderId = david.Id,
            ReceiverId = usuario.Id,
            IsAccepted = true
        };

        var friendRequest_2 = new FriendRequest
        {
            SenderId = david.Id,
            ReceiverId = samuel.Id,
            IsAccepted = true
        };

        var friendRequest_3 = new FriendRequest
        {
            SenderId = david.Id,
            ReceiverId = manolo.Id,
            IsAccepted = true
        };

        var friendRequest_4 = new FriendRequest
        {
            SenderId = david.Id,
            ReceiverId = pedro.Id,
            IsAccepted = true
        };

        var friendRequest_5 = new FriendRequest
        {
            SenderId = david.Id,
            ReceiverId = daniel.Id,
            IsAccepted = true
        };

        await _context.FriendRequest.AddRangeAsync(
            friendRequest_1, 
            friendRequest_2, 
            friendRequest_3, 
            friendRequest_4,
            friendRequest_5
            );

        // Crear historial de partidas
        var gameHistories = new List<GameHistory>
        {
            new GameHistory
            {
                GameName = "Juego de Memoria",
                Score = 8,
                OpponentScore = 0,
                Players = $"{david.Nickname}, {usuario.Nickname}",
                Result = "Ganador",
                Duration = TimeSpan.FromMinutes(15),
                UserId = david.Id
            },
            new GameHistory
            {
                GameName = "Juego de Memoria",
                Score = 2,
                OpponentScore = 6,
                Players = $"{david.Nickname}, {samuel.Nickname}",
                Result = "Perdedor",
                Duration = TimeSpan.FromMinutes(25),
                UserId = david.Id
            },
            new GameHistory
            {
                GameName = "Juego de Memoria",
                Score = 5,
                OpponentScore = 3,
                Players = $"{david.Nickname}, {manolo.Nickname}",
                Result = "Ganador",
                Duration = TimeSpan.FromMinutes(2),
                UserId = david.Id
            },
            new GameHistory
            {
                GameName = "Juego de Memoria",
                Score = 1,
                OpponentScore = 7,
                Players = $"{david.Nickname}, {daniel.Nickname}",
                Result = "Perdedor",
                Duration = TimeSpan.FromMinutes(3),
                UserId = david.Id
            },
            new GameHistory
            {
                GameName = "Juego de Memoria",
                Score = 2,
                OpponentScore = 6,
                Players = $"{david.Nickname}, {pedro.Nickname}",
                Result = "Perdedor",
                Duration = TimeSpan.FromMinutes(5),
                UserId = david.Id
            },
            new GameHistory
            {
                GameName = "Juego de Memoria",
                Score = 7,
                OpponentScore = 1,
                Players = $"{david.Nickname}, {usuario.Nickname}",
                Result = "Ganador",
                Duration = TimeSpan.FromMinutes(7),
                UserId = david.Id
            },
            new GameHistory
            {
                GameName = "Juego de Memoria",
                Score = 5,
                OpponentScore = 3,
                Players = $"{david.Nickname}, {samuel.Nickname}",
                Result = "Ganador",
                Duration = TimeSpan.FromMinutes(4),
                UserId = david.Id
            }
        };

        await _context.GameHistory.AddRangeAsync(gameHistories);
        await _context.SaveChangesAsync();
    }
}