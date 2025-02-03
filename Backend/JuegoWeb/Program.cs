using JuegoWeb.Middlewares;
using JuegoWeb.Models.Database;
using JuegoWeb.Models.Database.Repositories.Implementations;
using JuegoWeb.Models.Mappers;
using JuegoWeb.Services;
using JuegoWeb.WebSocketAdvanced;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

namespace JuegoWeb
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuración del directorio
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            // Leer la configuración
            builder.Services.Configure<Settings>(builder.Configuration.GetSection(Settings.SECTION_NAME));

            // Inyectamos el DbContext
            builder.Services.AddScoped<JuegoWebContext>();
            builder.Services.AddScoped<UnitOfWork>();

            // Inyección de todos los repositorios
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<ImageRepository>();
            builder.Services.AddScoped<FriendRequestRepository>();
            builder.Services.AddScoped<UserFriendRepository>();

            // Inyección de Mappers
            builder.Services.AddScoped<UserMapper>();
            builder.Services.AddScoped<ImageMapper>();
            builder.Services.AddScoped<FriendRequestMapper>();

            // Inyección de Servicios
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<ImageService>();
            builder.Services.AddScoped<FriendRequestService>();
            builder.Services.AddSingleton<WebSocketNetwork>();
            builder.Services.AddSingleton<IWebSocketMessageSender, WebSocketNetwork>();
            builder.Services.AddSingleton<WebSocketNotificationService>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configuración de CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins", builder =>
                {
                    builder.AllowAnyOrigin() // Permitir cualquier origen
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });

            // Añadir controladores
            builder.Services.AddControllers();
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

            // Configuración de autenticación
            builder.Services.AddAuthentication()
                .AddJwtBearer(options =>
                {
                    Settings settings = builder.Configuration.GetSection(Settings.SECTION_NAME).Get<Settings>();
                    string key = settings.JwtKey;

                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                    };
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                // Permite CORS
                app.UseCors("AllowAllOrigins");
            }

            // wwwroot
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
            });

            // Habilita el uso de websockets
            app.UseWebSockets();

            // Middleware que convierte CONNECT a GET
            app.UseMiddleware<WebSocketGetMiddleware>();

            // Middleware que agrega el JWT al encabezado de autorización
            app.UseMiddleware<WebSocketTokenMiddleware>();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            await SeedDataBaseAsync(app.Services);

            app.Run();
        }

        // Seeder
        static async Task SeedDataBaseAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            using JuegoWebContext dbContext = scope.ServiceProvider.GetService<JuegoWebContext>();

            // Si no existe la base de datos, la creamos y ejecutamos el seeder
            if (dbContext.Database.EnsureCreated())
            {
                Seeder seeder = new Seeder(dbContext);
                await seeder.SeedAsync();
            }
        }
    }
}