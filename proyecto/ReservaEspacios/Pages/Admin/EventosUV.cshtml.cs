using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;

namespace ReservaEspacios.Pages.Admin;

public class EventosUVModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ReservaService _reservas;
    public EventosUVModel(AppDbContext db, ReservaService reservas) { _db = db; _reservas = reservas; }

    public class Solicitud
    {
        public int IdReservacion { get; set; }     // representante (para resolver)
        public int? IdGrupo { get; set; }
        public string Usuario { get; set; } = "";
        public string Espacio { get; set; } = "";
        public string Motivo { get; set; } = "";
        public string Horario { get; set; } = "";
        public List<DateOnly> Dias { get; set; } = new();
    }

    public List<Solicitud> Solicitudes { get; set; } = new();

    public void OnGet()
    {
        var pendientes = _db.Reservaciones
            .Include(r => r.Espacio)
            .Include(r => r.Usuario)
            .Where(r => r.EstadoAprobacion == EstadoAprobacion.Pendiente)
            .ToList();

        // Agrupa por serie (IdGrupoReserva); las individuales por su propio id (negativo para no chocar).
        Solicitudes = pendientes
            .GroupBy(r => r.IdGrupoReserva ?? -r.IdReservacion)
            .Select(g =>
            {
                var primera = g.OrderBy(x => x.Fecha).First();
                return new Solicitud
                {
                    IdReservacion = primera.IdReservacion,
                    IdGrupo = primera.IdGrupoReserva,
                    Usuario = primera.Usuario?.Nombre ?? "",
                    Espacio = primera.Espacio?.Nombre ?? "",
                    Motivo = primera.Motivo,
                    Horario = $"{Fmt.Hm(primera.HoraInicio)}–{Fmt.Hm(primera.HoraFin)}",
                    Dias = g.OrderBy(x => x.Fecha).Select(x => x.Fecha).ToList()
                };
            })
            .OrderBy(s => s.Dias.First())
            .ToList();
    }

    public IActionResult OnPostResolver(int id, bool aprobar)
    {
        var r = _reservas.ResolverUV(id, aprobar);
        TempData[r.Exito ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToPage();
    }
}
