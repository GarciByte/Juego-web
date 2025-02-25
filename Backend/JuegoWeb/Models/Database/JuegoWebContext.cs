using JuegoWeb.Models.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace JuegoWeb.Models.Database;

public class JuegoWebContext : DbContext
{
    private const string DATABASE_PATH = "JuegoWeb.db";

    // Tablas
    public DbSet<Image> Images { get; set; }

    public DbSet<User> User { get; set; }

    public DbSet<FriendRequest> FriendRequest { get; set; }

    public DbSet<UserFriend> UserFriends { get; set; }

    public DbSet<GameHistory> GameHistory { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#if DEBUG
        string basedir = AppDomain.CurrentDomain.BaseDirectory;
        optionsBuilder.UseSqlite($"DataSource={basedir}{DATABASE_PATH}");
#else
        string connection = "Server=db14512.databaseasp.net; Database=db14512; Uid=db14512; Pwd=c@8D_4Aq2g=W;";
        optionsBuilder.UseMySql(connection, ServerVersion.AutoDetect(connection));
#endif
    }
}