using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using angeAuth.Models;
using AngeAuth.Data;
using AngeAuth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AngeAuth.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }


        [Authorize]
        [HttpGet("suscripcion")]
        public async Task<IActionResult> GetSuscripcion()
        {
            try
            {
                // Leer ID del usuario desde el JWT
                var userIdClaim = User.Claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Sub ||
                c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Token inválido" });

                Guid userId = Guid.Parse(userIdClaim);
                var subscription = await _db.Subscriptions
                    .Include(s => s.Plan)
                    .Where(s => s.UsuarioId == userId && s.Activo)
                    .OrderByDescending(s => s.FechaFin)
                    .FirstOrDefaultAsync();

                if (subscription == null)
                    return NotFound(new { message = "No hay suscripción activa" });

                // ✅ Buscar la empresa asociada al usuario
                var empresa = await _db.Empresas
                    .Where(e => e.UsuarioId == userId)
                    .FirstOrDefaultAsync();

                var hoy = DateTime.UtcNow;
                var mesesRestantes = Math.Max(0, ((subscription.FechaFin.Year - hoy.Year) * 12) + subscription.FechaFin.Month - hoy.Month);

                // ✅ Retornar la suscripción y los datos de la empresa
                return Ok(new
                {
                    empresa = empresa == null ? null : new
                    {
                        empresa.Nombre,
                        empresa.RUC,
                        empresa.Direccion,
                        empresa.PagoMinimo
                    },
                    suscripcion = new
                    {
                        subscription.Id,
                        mesesRestantes,
                        fechaInicio = subscription.FechaInicio,
                        fechaFin = subscription.FechaFin,
                        activo = subscription.Activo,
                        plan = subscription.Plan?.Nombre,
                        monto = subscription.Plan?.Monto ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener la suscripción", error = ex.Message });
            }
        }






        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // 1️⃣ Verificar si existe usuario
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Username))
                return BadRequest(new { message = "Email o usuario ya existe" });

            // 2️⃣ Validar cupón
            bool cuponValido = false;
            Cupon? usedCupon = null;
            if (!string.IsNullOrEmpty(dto.CodigoCupon))
            {
                usedCupon = await _db.Cupones.FirstOrDefaultAsync(c => c.Codigo == dto.CodigoCupon && c.Activo && c.UsosActuales < c.UsosMaximos);
                if (usedCupon != null)
                {
                    cuponValido = true;
                    usedCupon.UsosActuales++;
                    _db.Cupones.Update(usedCupon);
                }
            }

            // 3️⃣ Validar pago mínimo o plan
            if (!cuponValido && (dto.PagoMinimo == null || dto.PagoMinimo <= 0) && dto.PlanNombre == null)
                return BadRequest(new { message = "Pago mínimo o plan requerido si no hay cupón" });

            // 4️⃣ Crear usuario (maestro o normal)
            var user = new User
            {
                Email = dto.Email,
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow,
                Active = dto.IsMasterActive ?? true, // Maestro activo/inactivo
                IsMaster = dto.IsMaster ?? false
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 5️⃣ Crear empresa
            var empresa = new Empresa
            {
                Nombre = dto.NombreEmpresa,
                RUC = dto.RUC ?? "",
                Direccion = dto.Direccion ?? "",
                PagoMinimo = cuponValido ? 0 : (dto.PagoMinimo ?? 0m),
                UsuarioId = user.Id
            };
            _db.Empresas.Add(empresa);
            await _db.SaveChangesAsync();

            // 6️⃣ Plan y suscripción
            Plan? plan = null;
            Subscription? subscription = null;
            decimal planMonto = 0m;
            int periodoMeses = dto.PeriodoMeses ?? 1;

            if (!string.IsNullOrEmpty(dto.PlanNombre))
            {
                plan = await _db.Planes.Include(p => p.Vistas).ThenInclude(v => v.SubVistas)
                                        .FirstOrDefaultAsync(p => p.Nombre == dto.PlanNombre);

                if (plan == null)
                {
                    plan = new Plan
                    {
                        Nombre = dto.PlanNombre,
                        Monto = dto.PagoMinimo ?? 0m,
                        PeriodoMeses = dto.PeriodoMeses ?? 1,
                        Vistas = new List<PlanVista>()  // Aquí EF espera PlanVista
                    };

                    var planVistas = dto.PlanVistas ?? new List<PlanVistaDto>();

                    foreach (var pv in planVistas)
                    {
                        var planVista = new PlanVista
                        {
                            Nombre = pv.Nombre,
                            Activo = true,
                            SubVistas = pv.SubVistas?.Select(s => new PlanSubVista
                            {
                                Nombre = s,
                                Activo = true
                            }).ToList() ?? new List<PlanSubVista>()
                        };
                        plan.Vistas.Add(planVista);
                    }

                    _db.Planes.Add(plan);
                    await _db.SaveChangesAsync();
                }

                planMonto = plan.Monto;
                periodoMeses = plan.PeriodoMeses;
            }
            else if (dto.PagoMinimo.HasValue && dto.PagoMinimo.Value > 0)
            {
                planMonto = dto.PagoMinimo.Value;
            }

            // Suscripción
            subscription = new Subscription
            {
                UsuarioId = user.Id,
                PlanId = plan?.Id,
                FechaInicio = dto.FechaInicio ?? DateTime.UtcNow,
                FechaFin = dto.FechaFin ?? (dto.FechaInicio ?? DateTime.UtcNow).AddMonths(periodoMeses),
                FechaProximoCobro = (dto.FechaInicio ?? DateTime.UtcNow).AddMonths(periodoMeses),
                Activo = true
            };
            _db.Subscriptions.Add(subscription);
            await _db.SaveChangesAsync();

            // 7️⃣ Cargos variables
            decimal totalVariablesMes0 = 0m;
            if (dto.VariableCharges != null && dto.VariableCharges.Any())
            {
                foreach (var v in dto.VariableCharges)
                {
                    var typeParsed = Enum.TryParse<VariableChargeType>(v.Type, true, out var t) ? t : VariableChargeType.Other;
                    var vc = new VariableCharge
                    {
                        SubscriptionId = subscription.Id,
                        Type = typeParsed,
                        Description = v.Description,
                        Amount = v.Amount ?? 0m,
                        Hours = v.Hours,
                        RatePerHour = v.RatePerHour,
                        MonthOffset = v.MonthOffset
                    };
                    _db.VariableCharges.Add(vc);

                    if (vc.MonthOffset == 0)
                        totalVariablesMes0 += vc.EffectiveAmount();
                }
                await _db.SaveChangesAsync();
            }

            // 8️⃣ Registrar último monto cobrado
            subscription.UltimoMontoCobrado = (cuponValido ? 0m : planMonto) + totalVariablesMes0;
            _db.Subscriptions.Update(subscription);
            await _db.SaveChangesAsync();

            // 9️⃣ Crear vistas/subvistas del usuario
            var userVistas = dto.UserVistas ?? new List<UserVistaDto>();

            foreach (var uv in userVistas)
            {
                var vista = new Vista
                {
                    Nombre = uv.Nombre,
                    Activo = true,
                    UsuarioId = user.Id
                };
                _db.Vistas.Add(vista);
                await _db.SaveChangesAsync();

                var subVistas = uv.SubVistas ?? new List<string>();
                foreach (var s in subVistas)
                {
                    _db.SubVistas.Add(new SubVista
                    {
                        Nombre = s,
                        VistaId = vista.Id,
                        Activo = true
                    });
                }
                await _db.SaveChangesAsync();
            }


            //  🔟 Generar token
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                message = "Usuario registrado, empresa creada, suscripción y cargos variables procesados",
                cuponUsado = cuponValido,
                subscriptionId = subscription.Id
            });
        }




        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Credenciales inválidas" });

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Credenciales inválidas" });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                isMaster = user.IsMaster,
                activo = user.Active
            });
        }


        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"]!));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public record RegisterDto(
      string Username,
      string Email,
      string Password,
      string NombreEmpresa,
      string? RUC = null,
      string? Direccion = null,
      decimal? PagoMinimo = null,
      string? CodigoCupon = null,
      bool? IsMaster = null,                 // Si es usuario maestro
      bool? IsMasterActive = null,           // Estado del usuario maestro
      string? PlanNombre = null,             // Nombre del plan (si no existe se crea)
      int? PeriodoMeses = null,              // Duración en meses
      DateTime? FechaInicio = null,          // Inicio de la suscripción
      DateTime? FechaFin = null,             // Fin de la suscripción
      List<PlanVistaDto>? PlanVistas = null, // Vistas y sub-vistas asociadas al plan
      List<VariableChargeDto>? VariableCharges = null, // Cargos variables
      List<UserVistaDto>? UserVistas = null  // Vistas/subvistas específicas del usuario
  );

        public record PlanVistaDto(
       string Nombre,
       List<string>? SubVistas
   );

        public record UserVistaDto(
            string Nombre,
            List<string>? SubVistas
        );

        public record VariableChargeDto(
            string Type,        // "AwsService" | "Electricity" | "ReviewTime" | "Other"
            string? Description,
            decimal? Amount,
            decimal? Hours,
            decimal? RatePerHour,
            int MonthOffset = 0
        );



        public record LoginDto(string Email, string Password);
    }
}
