using FluentAssertions;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;
using Xunit;

namespace ReservaEspacios.Tests;

// CP-09 — CONSULTA / conteo dentro de la ventana de 30 días
public class InasistenciaServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Usuario _usuario;
    private readonly Espacio _espacio;

    public InasistenciaServiceTests()
    {
        _db = TestDb.Crear();
        (_usuario, _espacio) = TestDb.Sembrar(_db);
    }

    public void Dispose() => _db.Dispose();

    private Reservacion Falta(DateOnly fecha, TipoEvento tipo, ResultadoUso uso) => new()
    {
        IdUsuario = _usuario.IdUsuario,
        IdEspacio = _espacio.IdEspacio,
        TipoEvento = tipo,
        Fecha = fecha,
        HoraInicio = new TimeOnly(10, 0),
        HoraFin = new TimeOnly(11, 0),
        Motivo = "x",
        EstadoReservacion = EstadoReservacion.Finalizada,
        EstadoAprobacion = tipo == TipoEvento.UV ? EstadoAprobacion.Aprobada : EstadoAprobacion.NA,
        SeRealizo = uso
    };

    [Fact]
    public void FaltasUltimos30_TresFaltasNormalesRecientes_RetornaTresYSugiereBloqueo()
    {
        // Arrange
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        // 3 faltas Normal dentro de la ventana (sí cuentan)
        _db.Reservaciones.Add(Falta(hoy.AddDays(-2), TipoEvento.Normal, ResultadoUso.NoRealizada));
        _db.Reservaciones.Add(Falta(hoy.AddDays(-5), TipoEvento.Normal, ResultadoUso.NoRealizada));
        _db.Reservaciones.Add(Falta(hoy.AddDays(-10), TipoEvento.Normal, ResultadoUso.NoRealizada));
        // 1 falta vieja (fuera de 30 días) — NO cuenta
        _db.Reservaciones.Add(Falta(hoy.AddDays(-40), TipoEvento.Normal, ResultadoUso.NoRealizada));
        // 1 evento UV no realizado — NO cuenta para inasistencias
        _db.Reservaciones.Add(Falta(hoy.AddDays(-3), TipoEvento.UV, ResultadoUso.NoRealizada));
        // 1 reservación realizada — NO cuenta
        _db.Reservaciones.Add(Falta(hoy.AddDays(-4), TipoEvento.Normal, ResultadoUso.Realizada));
        _db.SaveChanges();

        var servicio = new InasistenciaService(_db);

        // Act
        var faltas = servicio.FaltasUltimos30(_usuario.IdUsuario);
        var sugiere = servicio.SugerirBloqueo(_usuario.IdUsuario);

        // Assert
        faltas.Should().Be(3);
        sugiere.Should().BeTrue();
    }
}

// CP-10 — Prueba unitaria pura (sin base de datos): hash de contraseñas
public class PasswordHasherTests
{
    [Fact]
    public void Verify_PasswordCorrectaEIncorrecta_DevuelveTrueYFalse()
    {
        // Arrange
        var hash = PasswordHasher.Hash("Secreta123");

        // Act
        var correcta = PasswordHasher.Verify("Secreta123", hash);
        var incorrecta = PasswordHasher.Verify("OtraClave", hash);

        // Assert
        correcta.Should().BeTrue();
        incorrecta.Should().BeFalse();
    }
}
