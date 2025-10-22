using angeAuth.Models;
using AngeAuth.Models;
using Microsoft.EntityFrameworkCore;

namespace AngeAuth.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Tablas
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Empresa> Empresas { get; set; } = null!;
        public DbSet<Cupon> Cupones { get; set; } = null!;
        public DbSet<Plan> Planes { get; set; } = null!;
        public DbSet<Vista> Vistas { get; set; } = null!;
        public DbSet<SubVista> SubVistas { get; set; } = null!;
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public DbSet<VariableCharge> VariableCharges { get; set; } = null!;
        public DbSet<PlanVista> PlanVistas { get; set; } = null!;
        public DbSet<PlanSubVista> PlanSubVistas { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Forzar nombres de tabla en minúscula
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Empresa>().ToTable("empresas");
            modelBuilder.Entity<Cupon>().ToTable("cupones");
            modelBuilder.Entity<Plan>().ToTable("planes");
            modelBuilder.Entity<Vista>().ToTable("vistas");
            modelBuilder.Entity<SubVista>().ToTable("subvistas");
            modelBuilder.Entity<Subscription>().ToTable("subscriptions");
            modelBuilder.Entity<VariableCharge>().ToTable("variablecharges");

            // Índices y relaciones (ejemplos)
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

            modelBuilder.Entity<Empresa>()
                .HasOne(e => e.Usuario)
                .WithMany(u => u.Empresas)
                .HasForeignKey(e => e.UsuarioId);

            modelBuilder.Entity<Cupon>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Cupones)
                .HasForeignKey(c => c.UsuarioId);

            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Usuario)
                .WithMany()
                .HasForeignKey(s => s.UsuarioId);

            modelBuilder.Entity<VariableCharge>()
                .HasOne(vc => vc.Subscription)
                .WithMany()
                .HasForeignKey(vc => vc.SubscriptionId);

            modelBuilder.Entity<SubVista>()
       .HasOne(sv => sv.Vista)
       .WithMany(v => v.SubVistas)
       .HasForeignKey(sv => sv.VistaId)
       .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PlanVista>()
                .ToTable("planvistas")
                .HasOne(pv => pv.Plan)
                .WithMany(p => p.Vistas)
                .HasForeignKey(pv => pv.PlanId);

            modelBuilder.Entity<PlanSubVista>()
                .ToTable("plansubvistas")
                .HasOne(psv => psv.PlanVista)
                .WithMany(pv => pv.SubVistas)
                .HasForeignKey(psv => psv.PlanVistaId);

        }
    }
}
