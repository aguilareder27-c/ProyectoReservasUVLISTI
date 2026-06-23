using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;

namespace ReservaEspacios.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ReservaService _reservas;
    public IndexModel(AppDbContext db, ReservaService reservas) { _db = db; _reservas = reservas; }

    public int UVPendientes { get; set; }
    public int ApelacionesPendientes { get; set; }
    public int TotalUsuarios { get; set; }
    public int TotalEspacios { get; set; }
    public int ReservasHoy { get; set; }

    public void OnGet()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        UVPendientes = _db.Reservaciones.Count(r => r.EstadoAprobacion == EstadoAprobacion.Pendiente);
        ApelacionesPendientes = _db.Apelaciones.Count(a => a.EstadoApelacion == EstadoApelacion.Pendiente);
        TotalUsuarios = _db.Usuarios.Count();
        TotalEspacios = _db.Espacios.Count();
        ReservasHoy = _db.Reservaciones.Count(r => r.Fecha == hoy && r.EstadoReservacion == EstadoReservacion.Activa);
    }

    public IActionResult OnPostProcesar()
    {
        int n = _reservas.ProcesarExpiraciones();
        TempData["Ok"] = $"Expiraciones procesadas. {n} reservación(es) marcada(s) como no realizada.";
        return RedirectToPage();
    }
}
