using System.ComponentModel.DataAnnotations;
using AngeAuth.Models;

namespace angeAuth.Models
{
    public class Cupon
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string Nombre { get; set; } = null!;
        [Required] public string Codigo { get; set; } = null!;
        public int UsosMaximos { get; set; } = 1;
        public int UsosActuales { get; set; } = 0;
        public bool Activo { get; set; } = true;

        public Guid UsuarioId { get; set; }
        public User Usuario { get; set; } = null!;
    }

}
