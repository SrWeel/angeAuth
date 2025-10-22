using System.ComponentModel.DataAnnotations;
using AngeAuth.Models;

namespace angeAuth.Models
{
    public class Empresa
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string Nombre { get; set; } = null!;
        [Required] public string RUC { get; set; } = null!;
        [Required] public string Direccion { get; set; } = null!;
        [Required] public decimal PagoMinimo { get; set; }

        public Guid UsuarioId { get; set; }
        public User Usuario { get; set; } = null!;
    }

}
