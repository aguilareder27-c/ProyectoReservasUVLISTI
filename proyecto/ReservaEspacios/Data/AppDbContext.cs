using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Models;

namespace ReservaEspacios.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<Espacio> Espacios => Set<Espacio>();
    public DbSet<DiaOperacionEspacio> DiasOperacion => Set<DiaOperacionEspacio>();
    public DbSet<FechaNoServicio> FechasNoServicio => Set<FechaNoServicio>();
    public DbSet<Reservacion> Reservaciones => Set<Reservacion>();
    public DbSet<Apelacion> Apelaciones => Set<Apelacion>();
    public DbSet<HistorialBloqueo> HistorialBloqueos => Set<HistorialBloqueo>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ----- USUARIO
        b.Entity<Usuario>().HasKey(x => x.IdUsuario);
        b.Entity<Usuario>().Property(x => x.Nombre).HasMaxLength(150);
        b.Entity<Usuario>().Property(x => x.Correo).HasMaxLength(150);
        b.Entity<Usuario>().Property(x => x.Contrasena).HasMaxLength(255);
        b.Entity<Usuario>().Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Entity<Usuario>().HasIndex(x => x.Correo).IsUnique();

        // ----- ADMINISTRADOR
        b.Entity<Administrador>().HasKey(x => x.IdAdmin);
        b.Entity<Administrador>().Property(x => x.Nombre).HasMaxLength(150);
        b.Entity<Administrador>().Property(x => x.Correo).HasMaxLength(150);
        b.Entity<Administrador>().Property(x => x.Contrasena).HasMaxLength(255);
        b.Entity<Administrador>().Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Entity<Administrador>().HasIndex(x => x.Correo).IsUnique();

        // ----- ESPACIO
        b.Entity<Espacio>().HasKey(x => x.IdEspacio);
        b.Entity<Espacio>().Property(x => x.Nombre).HasMaxLength(150);
        b.Entity<Espacio>().Property(x => x.Tipo).HasMaxLength(50);
        b.Entity<Espacio>().Property(x => x.Ubicacion).HasMaxLength(200);
        b.Entity<Espacio>().Property(x => x.Descripcion).HasMaxLength(500);
        b.Entity<Espacio>().Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);

        // ----- DIA_OPERACION_ESPACIO (clave compuesta)
        b.Entity<DiaOperacionEspacio>().HasKey(x => new { x.IdEspacio, x.DiaSemana });
        b.Entity<DiaOperacionEspacio>().Property(x => x.DiaSemana).HasConversion<string>().HasMaxLength(15);
        b.Entity<DiaOperacionEspacio>()
            .HasOne(x => x.Espacio).WithMany(e => e.DiasOperacion)
            .HasForeignKey(x => x.IdEspacio).OnDelete(DeleteBehavior.Cascade);

        // ----- FECHA_NO_SERVICIO
        b.Entity<FechaNoServicio>().HasKey(x => x.IdFechaNoServicio);
        b.Entity<FechaNoServicio>().Property(x => x.Motivo).HasMaxLength(300);
        b.Entity<FechaNoServicio>().Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30);
        b.Entity<FechaNoServicio>()
            .HasOne(x => x.Admin).WithMany()
            .HasForeignKey(x => x.IdAdmin).OnDelete(DeleteBehavior.Restrict);

        // ----- RESERVACION
        b.Entity<Reservacion>().HasKey(x => x.IdReservacion);
        b.Entity<Reservacion>().Property(x => x.Motivo).HasMaxLength(500);
        b.Entity<Reservacion>().Property(x => x.MotivoCancelacion).HasMaxLength(300);
        b.Entity<Reservacion>().Property(x => x.TipoEvento).HasConversion<string>().HasMaxLength(10);
        b.Entity<Reservacion>().Property(x => x.EstadoReservacion).HasConversion<string>().HasMaxLength(15);
        b.Entity<Reservacion>().Property(x => x.EstadoAprobacion).HasConversion<string>().HasMaxLength(15);
        b.Entity<Reservacion>().Property(x => x.SeRealizo).HasConversion<string>().HasMaxLength(15);
        b.Entity<Reservacion>()
            .HasOne(x => x.Usuario).WithMany(u => u.Reservaciones)
            .HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Reservacion>()
            .HasOne(x => x.Espacio).WithMany()
            .HasForeignKey(x => x.IdEspacio).OnDelete(DeleteBehavior.Restrict);

        // ----- APELACION
        b.Entity<Apelacion>().HasKey(x => x.IdApelacion);
        b.Entity<Apelacion>().Property(x => x.Justificacion).HasMaxLength(500);
        b.Entity<Apelacion>().Property(x => x.EstadoApelacion).HasConversion<string>().HasMaxLength(15);
        b.Entity<Apelacion>()
            .HasOne(x => x.Reservacion).WithMany(r => r.Apelaciones)
            .HasForeignKey(x => x.IdReservacion).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Apelacion>()
            .HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);

        // ----- HISTORIAL_BLOQUEO
        b.Entity<HistorialBloqueo>().HasKey(x => x.IdBloqueo);
        b.Entity<HistorialBloqueo>().Property(x => x.Motivo).HasMaxLength(300);
        b.Entity<HistorialBloqueo>().Property(x => x.TipoAccion).HasConversion<string>().HasMaxLength(15);
        b.Entity<HistorialBloqueo>()
            .HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HistorialBloqueo>()
            .HasOne(x => x.Admin).WithMany()
            .HasForeignKey(x => x.IdAdmin).OnDelete(DeleteBehavior.Restrict);
    }
}
