using ReservaEspacios.Models;

namespace ReservaEspacios.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (!db.Administradores.Any())
        {
            db.Administradores.Add(new Administrador
            {
                Nombre = "Administrador FEI",
                Correo = "admin@fei.uv.mx",
                Contrasena = Services.PasswordHasher.Hash("Admin123!"),
                Estado = EstadoUsuario.Activo
            });
        }

        if (!db.Espacios.Any())
        {
            var diasFEI = new[]
            {
                DiaSemana.Lunes, DiaSemana.Martes, DiaSemana.Miercoles,
                DiaSemana.Jueves, DiaSemana.Viernes
            };

            var auditorio = new Espacio
            {
                Nombre = "Auditorio",
                Tipo = "auditorio",
                Capacidad = 200,
                Ubicacion = "FEI, planta baja",
                Descripcion = "Auditorio principal de la Facultad de Estadística e Informática.",
                Estado = EstadoEspacio.Disponible,
                HoraApertura = new TimeOnly(7, 0),
                HoraCierre = new TimeOnly(21, 0)
            };
            foreach (var d in diasFEI) auditorio.DiasOperacion.Add(new DiaOperacionEspacio { DiaSemana = d });

            var salaCristal = new Espacio
            {
                Nombre = "Sala de Cristal",
                Tipo = "sala",
                Capacidad = 40,
                Ubicacion = "FEI, primer piso",
                Descripcion = "Sala de usos múltiples.",
                Estado = EstadoEspacio.Disponible,
                HoraApertura = new TimeOnly(7, 0),
                HoraCierre = new TimeOnly(20, 0)
            };
            foreach (var d in diasFEI) salaCristal.DiasOperacion.Add(new DiaOperacionEspacio { DiaSemana = d });

            db.Espacios.AddRange(auditorio, salaCristal);
        }

        db.SaveChanges();
    }
}
