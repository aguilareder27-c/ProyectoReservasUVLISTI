using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;

namespace ReservaEspacios.Pages.Admin;

public class CalendarioModel : PageModel
{
    private readonly AppDbContext _db;
    public CalendarioModel(AppDbContext db) { _db = db; }

    public DateOnly Lunes { get; set; }
    public List<DateOnly> Dias { get; set; } = new();
    public List<Espacio> Espacios { get; set; } = new();
    public List<Reservacion> Reservas { get; set; } = new();

    public void OnGet(string? semana)
    {
        DateOnly baseDate = Fmt.TryDate(semana, out var d) ? d : DateOnly.FromDateTime(DateTime.Now);

        // Retrocede al lunes de esa semana
        int delta = ((int)baseDate.DayOfWeek + 6) % 7; // lunes=0
        Lunes = baseDate.AddDays(-delta);
        Dias = Enumerable.Range(0, 7).Select(i => Lunes.AddDays(i)).ToList();
        var domingo = Lunes.AddDays(6);

        Espacios = _db.Espacios.OrderBy(e => e.Nombre).ToList();
        Reservas = _db.Reservaciones
            .Include(r => r.Usuario)
            .Where(r => r.Fecha >= Lunes && r.Fecha <= domingo
                        && (r.EstadoReservacion == EstadoReservacion.Activa
                            || r.EstadoReservacion == EstadoReservacion.Pendiente))
            .ToList();
    }

    public List<Reservacion> Celda(int idEspacio, DateOnly dia)
        => Reservas.Where(r => r.IdEspacio == idEspacio && r.Fecha == dia)
                   .OrderBy(r => r.HoraInicio).ToList();
}
