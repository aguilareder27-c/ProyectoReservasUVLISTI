using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;

namespace ReservaEspacios.Pages.Admin;

public class HistorialBloqueosModel : PageModel
{
    private readonly AppDbContext _db;
    public HistorialBloqueosModel(AppDbContext db) { _db = db; }

    public List<HistorialBloqueo> Historial { get; set; } = new();

    public void OnGet()
    {
        Historial = _db.HistorialBloqueos
            .Include(h => h.Usuario)
            .Include(h => h.Admin)
            .OrderByDescending(h => h.FechaAccion)
            .ToList();
    }
}
