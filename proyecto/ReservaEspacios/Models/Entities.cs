namespace ReservaEspacios.Models;

public class Usuario
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Contrasena { get; set; } = "";          // hash PBKDF2
    public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    // Las inasistencias NO se guardan como contador: se calculan del historial
    // dentro de la ventana de 30 días (ver InasistenciaService).
    public List<Reservacion> Reservaciones { get; set; } = new();
}

public class Administrador
{
    public int IdAdmin { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Contrasena { get; set; } = "";
    public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}

public class Espacio
{
    public int IdEspacio { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public int Capacidad { get; set; }
    public string Ubicacion { get; set; } = "";
    public string? Descripcion { get; set; }
    public EstadoEspacio Estado { get; set; } = EstadoEspacio.Disponible;
    public TimeOnly HoraApertura { get; set; }
    public TimeOnly HoraCierre { get; set; }

    public List<DiaOperacionEspacio> DiasOperacion { get; set; } = new();
}

// Qué días de la semana abre cada espacio. El horario lo toma de Espacio.
// Clave compuesta (IdEspacio, DiaSemana). Si no hay fila para un día, está cerrado ese día.
public class DiaOperacionEspacio
{
    public int IdEspacio { get; set; }
    public DiaSemana DiaSemana { get; set; }
    public Espacio? Espacio { get; set; }
}

// Días/periodos sin servicio a nivel facultad (global, aplica a todos los espacios).
public class FechaNoServicio
{
    public int IdFechaNoServicio { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }            // = FechaInicio si es un solo día
    public TimeOnly? HoraInicio { get; set; }          // null = día completo
    public TimeOnly? HoraFin { get; set; }             // null = día completo
    public TipoFechaNoServicio Tipo { get; set; }
    public string Motivo { get; set; } = "";
    public int IdAdmin { get; set; }
    public Administrador? Admin { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}

public class Reservacion
{
    public int IdReservacion { get; set; }
    public int IdUsuario { get; set; }
    public Usuario? Usuario { get; set; }
    public int IdEspacio { get; set; }
    public Espacio? Espacio { get; set; }
    public TipoEvento TipoEvento { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string Motivo { get; set; } = "";
    public EstadoReservacion EstadoReservacion { get; set; }
    public EstadoAprobacion EstadoAprobacion { get; set; }
    public ResultadoUso SeRealizo { get; set; } = ResultadoUso.Pendiente;
    public string? MotivoCancelacion { get; set; }

    // Agrupa las reservas creadas juntas (reserva de varios días / semana, solo UV).
    // null = reserva de un solo día.
    public int? IdGrupoReserva { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public List<Apelacion> Apelaciones { get; set; } = new();
}

public class Apelacion
{
    public int IdApelacion { get; set; }
    public int IdReservacion { get; set; }
    public Reservacion? Reservacion { get; set; }
    public int IdUsuario { get; set; }
    public Usuario? Usuario { get; set; }
    public string Justificacion { get; set; } = "";
    public EstadoApelacion EstadoApelacion { get; set; } = EstadoApelacion.Pendiente;
    public DateTime FechaApelacion { get; set; } = DateTime.Now;
    public DateTime? FechaResolucion { get; set; }
    public int? IdAdmin { get; set; }
}

public class HistorialBloqueo
{
    public int IdBloqueo { get; set; }
    public int IdUsuario { get; set; }
    public Usuario? Usuario { get; set; }
    public int IdAdmin { get; set; }
    public Administrador? Admin { get; set; }
    public TipoAccionBloqueo TipoAccion { get; set; }
    public string Motivo { get; set; } = "";
    public DateTime FechaAccion { get; set; } = DateTime.Now;
}
