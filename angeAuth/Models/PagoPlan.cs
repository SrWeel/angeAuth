using System.ComponentModel.DataAnnotations;
using AngeAuth.Models;

namespace angeAuth.Models
{
    public class PagoPlan
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UsuarioId { get; set; }
        public User Usuario { get; set; } = null!;
        public Guid PlanId { get; set; }
        public Plan Plan { get; set; } = null!;
        public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
        public DateTime FechaProximoCobro { get; set; }
        public bool Activo { get; set; } = true;
    }
}
