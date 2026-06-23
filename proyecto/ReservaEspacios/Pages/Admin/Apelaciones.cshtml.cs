using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;

namespace ReservaEspacios.Pages.Admin;

public class ApelacionesModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ReservaService _reservas;
    public ApelacionesModel(AppDbContext db, ReservaService reservas) { _db = db; _reservas = reservas; }

    public List<Apelacion> Pendientes { get; set; } = new();
    public List<Apelacion> Resueltas { get; set; } = new();

    public void OnGet()
    {
        var todas = _db.Apelaciones
            .Include(a => a.Usuario)
            .Include(a => a.Reservacion).ThenInclude(r => r!.Espacio)
            .OrderByDescending(a => a.FechaApelacion)
            .ToList();
        Pendientes = todas.Where(a => a.EstadoApelacion == EstadoApelacion.Pendiente).ToList();
        Resueltas = todas.Where(a => a.EstadoApelacion != EstadoApelacion.Pendiente).Take(20).ToList();
    }

    public IActionResult OnPostResolver(int id, bool aprobar)
    {
        var r = _reservas.ResolverApelacion(id, User.GetId(), aprobar);
        TempData[r.Exito ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToPage();
    }
}
