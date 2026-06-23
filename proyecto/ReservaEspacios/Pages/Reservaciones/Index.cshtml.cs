using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;

namespace ReservaEspacios.Pages.Reservaciones;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ReservaService _reservas;
    public IndexModel(AppDbContext db, ReservaService reservas) { _db = db; _reservas = reservas; }

    public List<Reservacion> Lista { get; set; } = new();

    public void OnGet()
    {
        var id = User.GetId();
        Lista = _db.Reservaciones
            .Include(r => r.Espacio)
            .Where(r => r.IdUsuario == id)
            .OrderByDescending(r => r.Fecha).ThenBy(r => r.HoraInicio)
            .ToList();
    }

    public IActionResult OnPostCancelar(int id, string motivo)
    {
        var r = _reservas.Cancelar(id, User.GetId(), motivo ?? "");
        TempData[r.Exito ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToPage();
    }

    public IActionResult OnPostCancelarSerie(int grupo, string motivo)
    {
        var r = _reservas.CancelarSerie(grupo, User.GetId(), motivo ?? "");
        TempData[r.Exito ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToPage();
    }

    public IActionResult OnPostConfirmar(int id)
    {
        var r = _reservas.ConfirmarUso(id, User.GetId());
        TempData[r.Exito ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToPage();
    }

    public IActionResult OnPostApelar(int id, string justificacion)
    {
        var r = _reservas.CrearApelacion(id, User.GetId(), justificacion ?? "");
        TempData[r.Exito ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToPage();
    }

    public static string Badge(EstadoReservacion e) => e switch
    {
        EstadoReservacion.Activa => "bg-success",
        EstadoReservacion.Pendiente => "bg-warning text-dark",
        EstadoReservacion.Cancelada => "bg-secondary",
        EstadoReservacion.Finalizada => "bg-dark",
        _ => "bg-light text-dark"
    };

    public static string BadgeUso(ResultadoUso u) => u switch
    {
        ResultadoUso.Realizada => "bg-success",
        ResultadoUso.NoRealizada => "bg-danger",
        _ => "bg-light text-dark"
    };

    public bool PuedeCancelar(Reservacion r)
        => (r.EstadoReservacion == EstadoReservacion.Activa || r.EstadoReservacion == EstadoReservacion.Pendiente)
           && DateTime.Now < r.Fecha.ToDateTime(r.HoraInicio);

    public bool PuedeConfirmar(Reservacion r)
        => r.EstadoReservacion == EstadoReservacion.Activa
           && r.SeRealizo == ResultadoUso.Pendiente
           && r.Espacio != null
           && DateTime.Now >= r.Fecha.ToDateTime(r.Espacio.HoraApertura)
           && DateTime.Now <= r.Fecha.ToDateTime(new TimeOnly(23, 59, 59));

    public bool PuedeApelar(Reservacion r) => r.SeRealizo == ResultadoUso.NoRealizada;
}
