
using JuegoWeb.Models.Database;
using JuegoWeb.Models.Database.Repositories.Implementations;
using JuegoWeb.Models.Mappers;
using JuegoWeb.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
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

            // Configuracion directorio
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            // Inyectamos el DbContext
            builder.Services.AddScoped<JuegoWebContext>();
            builder.Services.AddScoped<UnitOfWork>();

            // Inyección de todos los repositorios
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<ImageRepository>();

            // Inyección de Mappers
            builder.Services.AddScoped<UserMapper>();
            builder.Services.AddScoped<ImageMapper>();

            // Inyección de Servicios
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<ImageService>();

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

            builder.Services.AddControllers();
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

            // Configuración de autenticaci�n
            builder.Services.AddAuthentication()
                .AddJwtBearer(options =>
                {
                    string key = "A8$wX#pQ3dZ7v&kB1nY!rT@9mL%j6sHf4^g2Uc5*o";

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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHttpsRedirection();

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


