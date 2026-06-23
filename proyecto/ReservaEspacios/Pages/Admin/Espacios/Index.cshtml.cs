using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;

namespace ReservaEspacios.Pages.Admin.Espacios;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<Espacio> Espacios { get; set; } = new();

    public void OnGet()
    {
        Espacios = _db.Espacios.Include(e => e.DiasOperacion).OrderBy(e => e.Nombre).ToList();
    }

    public IActionResult OnPostToggle(int id)
    {
        var e = _db.Espacios.Find(id);
        if (e != null)
        {
            e.Estado = e.Estado == EstadoEspacio.Disponible ? EstadoEspacio.Inactivo : EstadoEspacio.Disponible;
            _db.SaveChanges();
            TempData["Ok"] = $"Espacio '{e.Nombre}' ahora está {e.Estado}.";
        }
        return RedirectToPage();
    }

    public static string Dias(Espacio e)
        => string.Join(", ", e.DiasOperacion.Select(d => d.DiaSemana).OrderBy(d => (int)d));
}
