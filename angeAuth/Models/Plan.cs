using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace angeAuth.Models
{
    public class Plan
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string Nombre { get; set; } = null!;
        [Required] public decimal Monto { get; set; }
        [Required] public int PeriodoMeses { get; set; }

        public ICollection<PlanVista> Vistas { get; set; } = new List<PlanVista>();
    }

    public class PlanVista
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string Nombre { get; set; } = null!;
        public bool Activo { get; set; } = true;

        public Guid PlanId { get; set; }
        public Plan Plan { get; set; } = null!;

        public ICollection<PlanSubVista> SubVistas { get; set; } = new List<PlanSubVista>();
    }

    public class PlanSubVista
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string Nombre { get; set; } = null!;
        public bool Activo { get; set; } = true;

        public Guid PlanVistaId { get; set; }
        public PlanVista PlanVista { get; set; } = null!;
    }

}
