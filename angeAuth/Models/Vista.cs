using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AngeAuth.Models
{
    public class Vista
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Nombre { get; set; } = null!;

        public bool Activo { get; set; } = true;

        // Relación al usuario propietario
        public Guid? UsuarioId { get; set; }
        public User? Usuario { get; set; }

        public ICollection<SubVista>? SubVistas { get; set; }
    }

    public class SubVista
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Nombre { get; set; } = null!;
        [ForeignKey("Vista")]
        public Guid VistaId { get; set; }
        public Vista Vista { get; set; } = null!;

        public bool Activo { get; set; } = true;
    }
}
