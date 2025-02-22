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

    // Crear archivo SQLite
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string basedir = AppDomain.CurrentDomain.BaseDirectory;
        optionsBuilder.UseSqlite($"DataSource={basedir}{DATABASE_PATH}");
    }
}