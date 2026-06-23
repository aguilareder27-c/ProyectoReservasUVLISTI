using ReservaEspacios.Data;
using ReservaEspacios.Models;

namespace ReservaEspacios.Services;

// Cuenta las inasistencias al vuelo dentro de una ventana móvil de 30 días.
// No hay contador almacenado: el historial es permanente, pero solo cuentan
// las faltas recientes. Solo aplica a eventos Normal.
public class InasistenciaService
{
    private readonly AppDbContext _db;
    public InasistenciaService(AppDbContext db) { _db = db; }

    public const int Umbral = 3;
    public const int DiasVentana = 30;

    public int FaltasUltimos30(int idUsuario)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var desde = hoy.AddDays(-DiasVentana);
        return _db.Reservaciones.Count(r => r.IdUsuario == idUsuario
            && r.TipoEvento == TipoEvento.Normal
            && r.SeRealizo == ResultadoUso.NoRealizada
            && r.Fecha >= desde && r.Fecha <= hoy);
    }

    public bool SugerirBloqueo(int idUsuario) => FaltasUltimos30(idUsuario) >= Umbral;
}
