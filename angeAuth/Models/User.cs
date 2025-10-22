using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using angeAuth.Models;

namespace AngeAuth.Models
{
    public class User
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required, MaxLength(100)] public string Username { get; set; } = null!;
        [Required, MaxLength(256)] public string Email { get; set; } = null!;
        [Required] public string PasswordHash { get; set; } = null!;
        public bool IsMaster { get; set; } = false;
        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Empresa>? Empresas { get; set; }
        public ICollection<Cupon>? Cupones { get; set; }
        public ICollection<Vista>? Vistas { get; set; }
    }

}
