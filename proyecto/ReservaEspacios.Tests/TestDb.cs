using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;

namespace ReservaEspacios.Tests;

// Utilidades para crear una base de datos en memoria (EF Core InMemory) y
// sembrar los datos mínimos que necesitan las pruebas.
public static class TestDb
{
    public static AppDbContext Crear()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // BD aislada por prueba
            .Options;
        return new AppDbContext(options);
    }

    // Siembra un espacio (Auditorio, lunes a viernes, 07:00–21:00),
    // un usuario activo y un administrador. Devuelve usuario y espacio.
    public static (Usuario usuario, Espacio espacio) Sembrar(AppDbContext db)
    {
        var espacio = new Espacio
        {
            Nombre = "Auditorio",
            Tipo = "auditorio",
            Capacidad = 200,
            Ubicacion = "FEI",
            Estado = EstadoEspacio.Disponible,
            HoraApertura = new TimeOnly(7, 0),
            HoraCierre = new TimeOnly(21, 0)
        };
        foreach (var d in new[] { DiaSemana.Lunes, DiaSemana.Martes, DiaSemana.Miercoles, DiaSemana.Jueves, DiaSemana.Viernes })
            espacio.DiasOperacion.Add(new DiaOperacionEspacio { DiaSemana = d });

        var usuario = new Usuario
        {
            Nombre = "Usuario Prueba",
            Correo = "prueba@uv.mx",
            Contrasena = "hash",
            Estado = EstadoUsuario.Activo
        };

        var admin = new Administrador
        {
            Nombre = "Admin Prueba",
            Correo = "admin@fei.uv.mx",
            Contrasena = "hash",
            Estado = EstadoUsuario.Activo
        };

        db.Espacios.Add(espacio);
        db.Usuarios.Add(usuario);
        db.Administradores.Add(admin);
        db.SaveChanges();

        return (usuario, espacio);
    }

    // Próximo día hábil (lunes a viernes) varios días hacia adelante,
    // para cumplir anticipación y que el espacio abra.
    public static DateOnly ProximoDiaHabil(int desdeHoy = 7)
    {
        var d = DateOnly.FromDateTime(DateTime.Now).AddDays(desdeHoy);
        while (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
            d = d.AddDays(1);
        return d;
    }

    // Próximo lunes (para rangos de varios días entre semana).
    public static DateOnly ProximoLunes(int semanas = 1)
    {
        var d = DateOnly.FromDateTime(DateTime.Now).AddDays(7 * semanas);
        while (d.DayOfWeek != DayOfWeek.Monday)
            d = d.AddDays(1);
        return d;
    }
}
