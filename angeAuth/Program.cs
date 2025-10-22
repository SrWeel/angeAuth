using System.Text;
using angeAuth.Models;
using AngeAuth.Data;
using AngeAuth.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

// Controllers
builder.Services.AddControllers();

// JWT
var jwtKey = configuration["Jwt:Key"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKey!);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };

    // Add events to log failures and successful validation to console for debugging
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (ctx.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = ctx.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    // Quitamos "Bearer " y asignamos solo el token
                    ctx.Token = authHeader.Substring("Bearer ".Length).Trim();
                    Console.WriteLine("[Jwt] Token correctly extracted from Authorization header");
                }
                else
                {
                    Console.WriteLine("[Jwt] Authorization header present but does not start with Bearer");
                }
            }
            else
            {
                Console.WriteLine("[Jwt] Authorization header missing");
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = ctx =>
        {
            Console.WriteLine("[Jwt] Authentication failed: " + ctx.Exception?.ToString());
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            var name = ctx.Principal?.Identity?.Name ?? "(no name)";
            Console.WriteLine($"[Jwt] Token validated. Name: {name}. Claims: {string.Join(", ", ctx.Principal?.Claims.Select(c => c.Type + ":" + c.Value) ?? new string[0])}");
            return Task.CompletedTask;
        }
    };

});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var app = builder.Build();

// ----------------------------
// Retry y migraciones
// ----------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var retries = 10;
    var success = false;

    while (retries > 0 && !success)
    {
        try
        {
            db.Database.Migrate(); // Aplica migraciones pendientes
            success = true;
        }
        catch (NpgsqlException)
        {
            retries--;
            Console.WriteLine("Postgres no listo aún, reintentando en 5s...");
            System.Threading.Thread.Sleep(5000);
        }
    }

    if (!success)
        throw new Exception("No se pudo conectar a PostgreSQL después de varios intentos");

    // ---------- Seed de usuario maestro, vistas y cupón ----------
    if (!db.Users.Any(u => u.IsMaster))
    {
        var master = new User
        {
            Username = "maestro",
            Email = "master@angeauth.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Losedd1456"),
            IsMaster = true,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(master);
        db.SaveChanges();

        var vistas = new List<string> { "dacturacion", "productos", "clientes", "creacion_empresa", "activacion_usuario" };
        foreach (var v in vistas)
        {
            db.Vistas.Add(new Vista { Nombre = v, Activo = true, UsuarioId = master.Id });
        }

        db.Cupones.Add(new Cupon
        {
            Nombre = "Daniela",
            Codigo = "DANIELA-001",
            UsosMaximos = 1,
            UsosActuales = 0,
            Activo = true,
            UsuarioId = master.Id
        });

        db.SaveChanges();
    }
}

// Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
