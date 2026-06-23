namespace ReservaEspacios.Models;

public enum EstadoUsuario { Activo, Bloqueado }

public enum EstadoEspacio { Disponible, Inactivo }

public enum DiaSemana { Lunes = 1, Martes = 2, Miercoles = 3, Jueves = 4, Viernes = 5, Sabado = 6, Domingo = 7 }

public enum TipoEvento { Normal, UV }

public enum EstadoReservacion { Activa, Cancelada, Finalizada, Pendiente }

// NA = no aplica (eventos Normal, aprobación automática)
public enum EstadoAprobacion { NA, Pendiente, Aprobada, Rechazada }

public enum ResultadoUso { Pendiente, Realizada, NoRealizada }

public enum TipoFechaNoServicio { Feriado, Vacaciones, Suspension, Mantenimiento, EventoInstitucional }

public enum EstadoApelacion { Pendiente, Aprobada, Rechazada }

public enum TipoAccionBloqueo { Bloqueo, Desbloqueo }
