using FluentAssertions;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;
using Xunit;

namespace ReservaEspacios.Tests;

// Pruebas del motor de reservaciones contra una base de datos (EF Core InMemory).
// Cada prueba usa una BD aislada que se prepara en el constructor (patrón del tutorial).
public class ReservaServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ReservaService _service;
    private readonly Usuario _usuario;
    private readonly Espacio _espacio;

    public ReservaServiceTests()
    {
        _db = TestDb.Crear();
        (_usuario, _espacio) = TestDb.Sembrar(_db);
        _service = new ReservaService(_db);
    }

    public void Dispose() => _db.Dispose();

    // CP-01 — INSERCIÓN / CONSULTA
    [Fact]
    public void CrearNormal_DatosValidos_InsertaReservacionActiva()
    {
        // Arrange
        var fecha = TestDb.ProximoDiaHabil();

        // Act
        var resultado = _service.CrearNormal(_usuario.IdUsuario, _espacio.IdEspacio,
            fecha, new TimeOnly(10, 0), new TimeOnly(12, 0), "Clase de prueba");

        // Assert
        resultado.Exito.Should().BeTrue();
        _db.Reservaciones.Should().ContainSingle();
        var reservacion = _db.Reservaciones.First();
        reservacion.EstadoReservacion.Should().Be(EstadoReservacion.Activa);
        reservacion.EstadoAprobacion.Should().Be(EstadoAprobacion.NA);
        reservacion.SeRealizo.Should().Be(ResultadoUso.Pendiente);
    }

    // CP-02 — CONSULTA (detección de traslape) / regla de negocio
    [Fact]
    public void CrearNormal_HorarioTraslapado_NoInsertaSegundaReservacion()
    {
        // Arrange: ya existe una reservación 10:00–12:00
        var fecha = TestDb.ProximoDiaHabil();
        _service.CrearNormal(_usuario.IdUsuario, _espacio.IdEspacio,
            fecha, new TimeOnly(10, 0), new TimeOnly(12, 0), "Primera");

        // Act: se intenta 11:00–13:00 (se traslapa)
        var resultado = _service.CrearNormal(_usuario.IdUsuario, _espacio.IdEspacio,
            fecha, new TimeOnly(11, 0), new TimeOnly(13, 0), "Segunda");

        // Assert
        resultado.Exito.Should().BeFalse();
        _db.Reservaciones.Count().Should().Be(1);
    }

    // CP-03 — INSERCIÓN MÚLTIPLE (reserva UV de varios días)
    [Fact]
    public void CrearUV_RangoDeVariosDias_InsertaUnaPorDiaConMismoGrupo()
    {
        // Arrange: lunes a miércoles (3 días hábiles)
        var lunes = TestDb.ProximoLunes();
        var miercoles = lunes.AddDays(2);

        // Act
        var resultado = _service.CrearUV(_usuario.IdUsuario, _espacio.IdEspacio,
            lunes, miercoles, new TimeOnly(10, 0), new TimeOnly(12, 0), "Congreso");

        // Assert
        resultado.Exito.Should().BeTrue();
        var reservas = _db.Reservaciones.Where(r => r.TipoEvento == TipoEvento.UV).ToList();
        reservas.Should().HaveCount(3);
        reservas.Select(r => r.IdGrupoReserva).Distinct().Should().ContainSingle();
        reservas.First().IdGrupoReserva.Should().NotBeNull();
        reservas.Should().OnlyContain(r => r.EstadoReservacion == EstadoReservacion.Pendiente
                                        && r.EstadoAprobacion == EstadoAprobacion.Pendiente);
    }

    // CP-04 — MODIFICACIÓN (cancelar)
    [Fact]
    public void Cancelar_ReservacionFutura_CambiaEstadoACancelada()
    {
        // Arrange
        var fecha = TestDb.ProximoDiaHabil();
        _service.CrearNormal(_usuario.IdUsuario, _espacio.IdEspacio,
            fecha, new TimeOnly(10, 0), new TimeOnly(12, 0), "Clase");
        var id = _db.Reservaciones.First().IdReservacion;

        // Act
        var resultado = _service.Cancelar(id, _usuario.IdUsuario, "Ya no se utilizará");

        // Assert
        resultado.Exito.Should().BeTrue();
        var reservacion = _db.Reservaciones.First();
        reservacion.EstadoReservacion.Should().Be(EstadoReservacion.Cancelada);
        reservacion.MotivoCancelacion.Should().Be("Ya no se utilizará");
    }

    // CP-05 — MODIFICACIÓN (autorización de evento UV)
    [Fact]
    public void ResolverUV_Aprobar_DejaReservacionActivaYAprobada()
    {
        // Arrange: una solicitud UV pendiente
        var fecha = TestDb.ProximoDiaHabil();
        _service.CrearUV(_usuario.IdUsuario, _espacio.IdEspacio,
            fecha, fecha, new TimeOnly(9, 0), new TimeOnly(13, 0), "Ceremonia");
        var id = _db.Reservaciones.First().IdReservacion;

        // Act
        var resultado = _service.ResolverUV(id, aprobar: true);

        // Assert
        resultado.Exito.Should().BeTrue();
        var reservacion = _db.Reservaciones.First();
        reservacion.EstadoReservacion.Should().Be(EstadoReservacion.Activa);
        reservacion.EstadoAprobacion.Should().Be(EstadoAprobacion.Aprobada);
    }

    // CP-06 — MODIFICACIÓN + CONSULTA (expiración automática)
    [Fact]
    public void ProcesarExpiraciones_ReservacionVencidaSinConfirmar_MarcaNoRealizada()
    {
        // Arrange: una reservación activa de ayer, sin confirmar
        var ayer = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
        _db.Reservaciones.Add(new Reservacion
        {
            IdUsuario = _usuario.IdUsuario,
            IdEspacio = _espacio.IdEspacio,
            TipoEvento = TipoEvento.Normal,
            Fecha = ayer,
            HoraInicio = new TimeOnly(10, 0),
            HoraFin = new TimeOnly(11, 0),
            Motivo = "Sesión pasada",
            EstadoReservacion = EstadoReservacion.Activa,
            EstadoAprobacion = EstadoAprobacion.NA,
            SeRealizo = ResultadoUso.Pendiente
        });
        _db.SaveChanges();

        // Act
        var marcadas = _service.ProcesarExpiraciones();

        // Assert
        marcadas.Should().Be(1);
        var reservacion = _db.Reservaciones.First();
        reservacion.SeRealizo.Should().Be(ResultadoUso.NoRealizada);
        reservacion.EstadoReservacion.Should().Be(EstadoReservacion.Finalizada);
    }

    // CP-07 — INSERCIÓN (historial) + MODIFICACIÓN (estado del usuario)
    [Fact]
    public void CambiarEstadoUsuario_Bloquear_BloqueaUsuarioYRegistraHistorial()
    {
        // Arrange
        var idAdmin = _db.Administradores.First().IdAdmin;

        // Act
        var resultado = _service.CambiarEstadoUsuario(_usuario.IdUsuario, idAdmin, bloquear: true, "3 inasistencias");

        // Assert
        resultado.Exito.Should().BeTrue();
        _db.Usuarios.First().Estado.Should().Be(EstadoUsuario.Bloqueado);
        _db.HistorialBloqueos.Should().ContainSingle();
        _db.HistorialBloqueos.First().TipoAccion.Should().Be(TipoAccionBloqueo.Bloqueo);
    }

    // CP-08 — ELIMINACIÓN (fecha sin servicio)
    [Fact]
    public void EliminarFechaNoServicio_ConRegistroExistente_LoElimina()
    {
        // Arrange
        var idAdmin = _db.Administradores.First().IdAdmin;
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var fecha = new FechaNoServicio
        {
            FechaInicio = hoy,
            FechaFin = hoy,
            Tipo = TipoFechaNoServicio.Feriado,
            Motivo = "Día festivo",
            IdAdmin = idAdmin
        };
        _db.FechasNoServicio.Add(fecha);
        _db.SaveChanges();
        _db.FechasNoServicio.Should().ContainSingle();

        // Act
        _db.FechasNoServicio.Remove(fecha);
        _db.SaveChanges();

        // Assert
        _db.FechasNoServicio.Should().BeEmpty();
    }
}
