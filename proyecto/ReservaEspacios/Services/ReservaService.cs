using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;

namespace ReservaEspacios.Services;

public class ResultadoOperacion
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = "";
    public List<string> Detalles { get; set; } = new();

    public static ResultadoOperacion Ok(string m, List<string>? d = null)
        => new() { Exito = true, Mensaje = m, Detalles = d ?? new() };

    public static ResultadoOperacion Error(string m, List<string>? d = null)
        => new() { Exito = false, Mensaje = m, Detalles = d ?? new() };
}

public class ReservaService
{
    private readonly AppDbContext _db;
    public ReservaService(AppDbContext db) { _db = db; }

    public const int MaxHorasNormal = 3;
    public const int MaxActivasNormal = 2;
    public const int AnticipacionMinHoras = 1;

    public static DiaSemana ToDiaSemana(DateOnly d) => d.DayOfWeek switch
    {
        DayOfWeek.Monday => DiaSemana.Lunes,
        DayOfWeek.Tuesday => DiaSemana.Martes,
        DayOfWeek.Wednesday => DiaSemana.Miercoles,
        DayOfWeek.Thursday => DiaSemana.Jueves,
        DayOfWeek.Friday => DiaSemana.Viernes,
        DayOfWeek.Saturday => DiaSemana.Sabado,
        _ => DiaSemana.Domingo
    };

    private static bool EspacioAbre(Espacio e, DateOnly fecha)
        => e.DiasOperacion.Any(d => d.DiaSemana == ToDiaSemana(fecha));

    // Devuelve la fecha de no servicio que bloquea (o null si no hay bloqueo).
    private static FechaNoServicio? FechaBloqueada(DateOnly fecha, TimeOnly hi, TimeOnly hf, List<FechaNoServicio> lista)
    {
        foreach (var f in lista)
        {
            if (fecha < f.FechaInicio || fecha > f.FechaFin) continue;
            if (f.HoraInicio is null || f.HoraFin is null) return f;          // día completo
            if (hi < f.HoraFin.Value && f.HoraInicio.Value < hf) return f;     // se traslapa con el rango de horas
        }
        return null;
    }

    // Reservaciones ACTIVAS que se traslapan con el horario solicitado en ese espacio y fecha.
    private List<Reservacion> Traslapes(int idEspacio, DateOnly fecha, TimeOnly hi, TimeOnly hf)
    {
        var delDia = _db.Reservaciones
            .Where(r => r.IdEspacio == idEspacio
                        && r.Fecha == fecha
                        && r.EstadoReservacion == EstadoReservacion.Activa)
            .ToList();
        return delDia.Where(r => hi < r.HoraFin && r.HoraInicio < hf).ToList();
    }

    // ---------------------------------------------------------------- EVENTO NORMAL
    public ResultadoOperacion CrearNormal(int idUsuario, int idEspacio, DateOnly fecha, TimeOnly hi, TimeOnly hf, string motivo)
    {
        var usuario = _db.Usuarios.Find(idUsuario);
        if (usuario is null) return ResultadoOperacion.Error("Usuario no encontrado.");
        if (usuario.Estado == EstadoUsuario.Bloqueado)
            return ResultadoOperacion.Error("Tu cuenta está bloqueada. Contacta al administrador para reactivarla.");

        var espacio = _db.Espacios.Include(e => e.DiasOperacion).FirstOrDefault(e => e.IdEspacio == idEspacio);
        if (espacio is null) return ResultadoOperacion.Error("Espacio no encontrado.");
        if (espacio.Estado != EstadoEspacio.Disponible) return ResultadoOperacion.Error("El espacio no está disponible.");
        if (hf <= hi) return ResultadoOperacion.Error("La hora de fin debe ser mayor a la de inicio.");
        if (!EspacioAbre(espacio, fecha)) return ResultadoOperacion.Error("El espacio no abre ese día de la semana.");

        var bloqueada = FechaBloqueada(fecha, hi, hf, _db.FechasNoServicio.ToList());
        if (bloqueada != null)
            return ResultadoOperacion.Error($"Fecha sin servicio ({bloqueada.Tipo}): {bloqueada.Motivo}.");

        if (hi < espacio.HoraApertura || hf > espacio.HoraCierre)
            return ResultadoOperacion.Error($"Fuera del horario de operación ({Fmt.Hm(espacio.HoraApertura)}–{Fmt.Hm(espacio.HoraCierre)}).");

        if ((hf - hi) > TimeSpan.FromHours(MaxHorasNormal))
            return ResultadoOperacion.Error($"Un evento Normal no puede durar más de {MaxHorasNormal} horas.");

        var inicio = fecha.ToDateTime(hi);
        if (inicio < DateTime.Now.AddHours(AnticipacionMinHoras))
            return ResultadoOperacion.Error($"Debes reservar con al menos {AnticipacionMinHoras} hora de anticipación.");

        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var activas = _db.Reservaciones.Count(r => r.IdUsuario == idUsuario
            && r.TipoEvento == TipoEvento.Normal
            && r.EstadoReservacion == EstadoReservacion.Activa
            && r.Fecha >= hoy);
        if (activas >= MaxActivasNormal)
            return ResultadoOperacion.Error($"Ya tienes {MaxActivasNormal} reservaciones Normales activas (máximo permitido).");

        var conflictos = Traslapes(idEspacio, fecha, hi, hf);
        if (conflictos.Count > 0)
            return ResultadoOperacion.Error("El horario se traslapa con otra reservación.",
                conflictos.Select(c => $"Ocupado {Fmt.Hm(c.HoraInicio)}–{Fmt.Hm(c.HoraFin)}").ToList());

        _db.Reservaciones.Add(new Reservacion
        {
            IdUsuario = idUsuario,
            IdEspacio = idEspacio,
            TipoEvento = TipoEvento.Normal,
            Fecha = fecha,
            HoraInicio = hi,
            HoraFin = hf,
            Motivo = motivo,
            EstadoReservacion = EstadoReservacion.Activa,
            EstadoAprobacion = EstadoAprobacion.NA,
            SeRealizo = ResultadoUso.Pendiente
        });
        _db.SaveChanges();
        return ResultadoOperacion.Ok("Reservación creada y aprobada automáticamente.");
    }

    // ---------------------------------------------------------------- EVENTO UV (uno o varios días)
    public ResultadoOperacion CrearUV(int idUsuario, int idEspacio, DateOnly fechaInicio, DateOnly fechaFin, TimeOnly hi, TimeOnly hf, string motivo)
    {
        var usuario = _db.Usuarios.Find(idUsuario);
        if (usuario is null) return ResultadoOperacion.Error("Usuario no encontrado.");
        if (usuario.Estado == EstadoUsuario.Bloqueado)
            return ResultadoOperacion.Error("Tu cuenta está bloqueada. Contacta al administrador.");

        var espacio = _db.Espacios.Include(e => e.DiasOperacion).FirstOrDefault(e => e.IdEspacio == idEspacio);
        if (espacio is null) return ResultadoOperacion.Error("Espacio no encontrado.");
        if (espacio.Estado != EstadoEspacio.Disponible) return ResultadoOperacion.Error("El espacio no está disponible.");
        if (fechaFin < fechaInicio) return ResultadoOperacion.Error("La fecha de fin no puede ser anterior a la de inicio.");
        if (hf <= hi) return ResultadoOperacion.Error("La hora de fin debe ser mayor a la de inicio.");
        if (hi < espacio.HoraApertura || hf > espacio.HoraCierre)
            return ResultadoOperacion.Error($"Fuera del horario de operación ({Fmt.Hm(espacio.HoraApertura)}–{Fmt.Hm(espacio.HoraCierre)}).");

        if (fechaInicio.ToDateTime(hi) < DateTime.Now)
            return ResultadoOperacion.Error("La fecha y hora de inicio ya pasaron.");

        var noServicio = _db.FechasNoServicio.ToList();
        var diasValidos = new List<DateOnly>();
        var omitidos = new List<string>();
        for (var d = fechaInicio; d <= fechaFin; d = d.AddDays(1))
        {
            if (!EspacioAbre(espacio, d)) { omitidos.Add($"{Fmt.Dmy(d)} — el espacio no abre"); continue; }
            var b = FechaBloqueada(d, hi, hf, noServicio);
            if (b != null) { omitidos.Add($"{Fmt.Dmy(d)} — sin servicio ({b.Tipo})"); continue; }
            diasValidos.Add(d);
        }
        if (diasValidos.Count == 0)
            return ResultadoOperacion.Error("No hay días válidos en el rango (todos cerrados o sin servicio).", omitidos);

        var conflictos = new List<string>();
        foreach (var d in diasValidos)
            if (Traslapes(idEspacio, d, hi, hf).Count > 0)
                conflictos.Add($"{Fmt.Dmy(d)} se traslapa con otra reservación.");
        if (conflictos.Count > 0)
            return ResultadoOperacion.Error("No se pudo crear la solicitud por traslapes:", conflictos);

        int? grupo = null;
        if (diasValidos.Count > 1)
            grupo = (_db.Reservaciones.Max(r => (int?)r.IdGrupoReserva) ?? 0) + 1;

        foreach (var d in diasValidos)
        {
            _db.Reservaciones.Add(new Reservacion
            {
                IdUsuario = idUsuario,
                IdEspacio = idEspacio,
                TipoEvento = TipoEvento.UV,
                Fecha = d,
                HoraInicio = hi,
                HoraFin = hf,
                Motivo = motivo,
                EstadoReservacion = EstadoReservacion.Pendiente,
                EstadoAprobacion = EstadoAprobacion.Pendiente,
                SeRealizo = ResultadoUso.Pendiente,
                IdGrupoReserva = grupo
            });
        }
        _db.SaveChanges();

        var detalles = omitidos.Count > 0 ? omitidos.Prepend("Días omitidos del rango:").ToList() : new List<string>();
        return ResultadoOperacion.Ok(
            $"Solicitud de Evento UV registrada ({diasValidos.Count} día(s)). Queda pendiente de autorización del administrador.",
            detalles);
    }

    // ---------------------------------------------------------------- CANCELAR
    public ResultadoOperacion Cancelar(int idReservacion, int idUsuario, string motivo)
    {
        var r = _db.Reservaciones.Find(idReservacion);
        if (r is null || r.IdUsuario != idUsuario) return ResultadoOperacion.Error("Reservación no encontrada.");
        if (r.EstadoReservacion is not (EstadoReservacion.Activa or EstadoReservacion.Pendiente))
            return ResultadoOperacion.Error("Esta reservación no se puede cancelar.");
        if (DateTime.Now >= r.Fecha.ToDateTime(r.HoraInicio))
            return ResultadoOperacion.Error("No se puede cancelar una reservación que ya inició.");

        r.EstadoReservacion = EstadoReservacion.Cancelada;
        r.MotivoCancelacion = motivo;
        _db.SaveChanges();
        return ResultadoOperacion.Ok("Reservación cancelada.");
    }

    public ResultadoOperacion CancelarSerie(int idGrupo, int idUsuario, string motivo)
    {
        var serie = _db.Reservaciones.Where(r => r.IdGrupoReserva == idGrupo && r.IdUsuario == idUsuario).ToList();
        if (serie.Count == 0) return ResultadoOperacion.Error("Serie no encontrada.");

        int n = 0;
        foreach (var r in serie)
        {
            if (r.EstadoReservacion is (EstadoReservacion.Activa or EstadoReservacion.Pendiente)
                && DateTime.Now < r.Fecha.ToDateTime(r.HoraInicio))
            {
                r.EstadoReservacion = EstadoReservacion.Cancelada;
                r.MotivoCancelacion = motivo;
                n++;
            }
        }
        _db.SaveChanges();
        return ResultadoOperacion.Ok($"Serie cancelada: {n} día(s).");
    }

    // ---------------------------------------------------------------- CONFIRMAR USO
    public ResultadoOperacion ConfirmarUso(int idReservacion, int idUsuario)
    {
        var r = _db.Reservaciones.Include(x => x.Espacio).FirstOrDefault(x => x.IdReservacion == idReservacion);
        if (r is null || r.IdUsuario != idUsuario) return ResultadoOperacion.Error("Reservación no encontrada.");
        if (r.EstadoReservacion != EstadoReservacion.Activa)
            return ResultadoOperacion.Error("Solo puedes confirmar reservaciones activas.");

        var inicioVentana = r.Fecha.ToDateTime(r.Espacio!.HoraApertura);
        var finVentana = r.Fecha.ToDateTime(new TimeOnly(23, 59, 59));
        var ahora = DateTime.Now;
        if (ahora < inicioVentana)
            return ResultadoOperacion.Error("Aún no inicia la ventana de confirmación (abre a la hora de apertura del espacio ese día).");
        if (ahora > finVentana)
            return ResultadoOperacion.Error("La ventana de confirmación de ese día ya cerró.");

        r.SeRealizo = ResultadoUso.Realizada;
        _db.SaveChanges();
        return ResultadoOperacion.Ok("Uso confirmado como realizado.");
    }

    // ---------------------------------------------------------------- EXPIRACIÓN AUTOMÁTICA
    // Marca como 'no realizada' las reservaciones activas de días ya pasados que no se confirmaron,
    // y las finaliza. (Las inasistencias se cuentan al vuelo, no hay contador que actualizar.)
    public int ProcesarExpiraciones()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var vencidas = _db.Reservaciones
            .Where(r => r.EstadoReservacion == EstadoReservacion.Activa && r.Fecha < hoy)
            .ToList();

        int marcadas = 0;
        foreach (var r in vencidas)
        {
            if (r.SeRealizo == ResultadoUso.Pendiente) { r.SeRealizo = ResultadoUso.NoRealizada; marcadas++; }
            r.EstadoReservacion = EstadoReservacion.Finalizada;
        }
        if (vencidas.Count > 0) _db.SaveChanges();
        return marcadas;
    }

    // ---------------------------------------------------------------- AUTORIZACIÓN UV (admin)
    public ResultadoOperacion ResolverUV(int idReservacion, bool aprobar)
    {
        var r = _db.Reservaciones.Find(idReservacion);
        if (r is null) return ResultadoOperacion.Error("Reservación no encontrada.");

        var afectadas = r.IdGrupoReserva is null
            ? new List<Reservacion> { r }
            : _db.Reservaciones.Where(x => x.IdGrupoReserva == r.IdGrupoReserva).ToList();

        foreach (var x in afectadas)
        {
            if (x.EstadoAprobacion != EstadoAprobacion.Pendiente) continue;

            if (!aprobar)
            {
                x.EstadoAprobacion = EstadoAprobacion.Rechazada;
                x.EstadoReservacion = EstadoReservacion.Cancelada;
                continue;
            }

            // Al aprobar, revalidar que no haya traslape con reservaciones ya activas.
            if (Traslapes(x.IdEspacio, x.Fecha, x.HoraInicio, x.HoraFin).Count > 0)
            {
                x.EstadoAprobacion = EstadoAprobacion.Rechazada;
                x.EstadoReservacion = EstadoReservacion.Cancelada;
                x.MotivoCancelacion = "Traslape detectado al momento de autorizar.";
            }
            else
            {
                x.EstadoAprobacion = EstadoAprobacion.Aprobada;
                x.EstadoReservacion = EstadoReservacion.Activa;
            }
        }
        _db.SaveChanges();
        return ResultadoOperacion.Ok(aprobar ? "Evento UV autorizado." : "Evento UV rechazado.");
    }

    // Corrección directa del estado de uso por el administrador.
    public ResultadoOperacion AdminCambiarUso(int idReservacion, ResultadoUso nuevo)
    {
        var r = _db.Reservaciones.Find(idReservacion);
        if (r is null) return ResultadoOperacion.Error("Reservación no encontrada.");
        r.SeRealizo = nuevo;
        _db.SaveChanges();
        return ResultadoOperacion.Ok("Estado de uso actualizado.");
    }

    // ---------------------------------------------------------------- APELACIONES
    public ResultadoOperacion CrearApelacion(int idReservacion, int idUsuario, string justificacion)
    {
        var r = _db.Reservaciones.Find(idReservacion);
        if (r is null || r.IdUsuario != idUsuario) return ResultadoOperacion.Error("Reservación no encontrada.");
        if (r.SeRealizo != ResultadoUso.NoRealizada)
            return ResultadoOperacion.Error("Solo puedes apelar reservaciones marcadas como 'no realizada'.");
        if (_db.Apelaciones.Any(a => a.IdReservacion == idReservacion && a.EstadoApelacion == EstadoApelacion.Pendiente))
            return ResultadoOperacion.Error("Ya existe una apelación pendiente para esta reservación.");

        _db.Apelaciones.Add(new Apelacion
        {
            IdReservacion = idReservacion,
            IdUsuario = idUsuario,
            Justificacion = justificacion
        });
        _db.SaveChanges();
        return ResultadoOperacion.Ok("Apelación enviada. El administrador la revisará.");
    }

    public ResultadoOperacion ResolverApelacion(int idApelacion, int idAdmin, bool aprobar)
    {
        var a = _db.Apelaciones.Include(x => x.Reservacion).FirstOrDefault(x => x.IdApelacion == idApelacion);
        if (a is null) return ResultadoOperacion.Error("Apelación no encontrada.");
        if (a.EstadoApelacion != EstadoApelacion.Pendiente) return ResultadoOperacion.Error("La apelación ya fue resuelta.");

        a.EstadoApelacion = aprobar ? EstadoApelacion.Aprobada : EstadoApelacion.Rechazada;
        a.FechaResolucion = DateTime.Now;
        a.IdAdmin = idAdmin;

        // Al aprobar, la reservación vuelve a 'realizada' y deja de contar como inasistencia.
        if (aprobar && a.Reservacion != null)
            a.Reservacion.SeRealizo = ResultadoUso.Realizada;

        _db.SaveChanges();
        return ResultadoOperacion.Ok(aprobar ? "Apelación aprobada; la falta se corrige." : "Apelación rechazada.");
    }

    // ---------------------------------------------------------------- BLOQUEO / DESBLOQUEO
    public ResultadoOperacion CambiarEstadoUsuario(int idUsuario, int idAdmin, bool bloquear, string motivo)
    {
        var u = _db.Usuarios.Find(idUsuario);
        if (u is null) return ResultadoOperacion.Error("Usuario no encontrado.");

        u.Estado = bloquear ? EstadoUsuario.Bloqueado : EstadoUsuario.Activo;
        _db.HistorialBloqueos.Add(new HistorialBloqueo
        {
            IdUsuario = idUsuario,
            IdAdmin = idAdmin,
            TipoAccion = bloquear ? TipoAccionBloqueo.Bloqueo : TipoAccionBloqueo.Desbloqueo,
            Motivo = motivo
        });
        _db.SaveChanges();
        return ResultadoOperacion.Ok(bloquear ? "Usuario bloqueado." : "Usuario desbloqueado.");
    }
}
