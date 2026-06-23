using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;

namespace ReservaEspacios.Pages.Espacios;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<Espacio> Espacios { get; set; } = new();

    public void OnGet()
    {
        Espacios = _db.Espacios
            .Include(e => e.DiasOperacion)
            .OrderBy(e => e.Nombre)
            .ToList();
    }

    public static string Dias(Espacio e)
    {
        var orden = e.DiasOperacion.Select(d => d.DiaSemana).OrderBy(d => (int)d);
        return string.Join(", ", orden);
    }
}
