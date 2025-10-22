using System.ComponentModel.DataAnnotations;
using angeAuth.Models;

namespace AngeAuth.Models
{
    public class Subscription
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UsuarioId { get; set; }
        public User Usuario { get; set; } = null!;
        public Guid? PlanId { get; set; }
        public Plan? Plan { get; set; }

        public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
        public DateTime FechaProximoCobro { get; set; }          // siguiente cobro
        public bool Activo { get; set; } = true;

        // Último monto cobrado (incluye variables)
        public decimal UltimoMontoCobrado { get; set; } = 0m;
        public DateTime FechaFin { get; internal set; }
    }
}
