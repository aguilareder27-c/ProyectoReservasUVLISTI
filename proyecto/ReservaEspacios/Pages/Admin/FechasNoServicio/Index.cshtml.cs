using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;

namespace ReservaEspacios.Pages.Admin.FechasNoServicio;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<ReservaEspacios.Models.FechaNoServicio> Fechas { get; set; } = new();

    [BindProperty] public string? FechaInicio { get; set; }
    [BindProperty] public string? FechaFin { get; set; }
    [BindProperty] public string? HoraInicio { get; set; }
    [BindProperty] public string? HoraFin { get; set; }
    [BindProperty] public string TipoStr { get; set; } = "Feriado";
    [BindProperty] public string Motivo { get; set; } = "";
    public string? Error { get; set; }

    public void OnGet()
    {
        Fechas = _db.FechasNoServicio
            .OrderByDescending(f => f.FechaInicio)
            .ToList();
    }

    public IActionResult OnPost()
    {
        if (!Fmt.TryDate(FechaInicio, out var fi))
        {
            TempData["Error"] = "Indica una fecha de inicio válida.";
            return RedirectToPage();
        }
        DateOnly ff = Fmt.TryDate(FechaFin, out var f2) ? f2 : fi;
        if (ff < fi)
        {
            TempData["Error"] = "La fecha de fin no puede ser anterior a la de inicio.";
            return RedirectToPage();
        }

        TimeOnly? hi = Fmt.TryTime(HoraInicio, out var t1) ? t1 : null;
        TimeOnly? hf = Fmt.TryTime(HoraFin, out var t2) ? t2 : null;
        // Si solo se da una de las dos horas, se ignora (se trata como día completo)
        if (hi is null || hf is null) { hi = null; hf = null; }
        if (hi != null && hf != null && hf <= hi)
        {
            TempData["Error"] = "La hora de fin debe ser mayor a la de inicio.";
            return RedirectToPage();
        }

        var tipo = Enum.TryParse<TipoFechaNoServicio>(TipoStr, out var t) ? t : TipoFechaNoServicio.Feriado;

        _db.FechasNoServicio.Add(new ReservaEspacios.Models.FechaNoServicio
        {
            FechaInicio = fi,
            FechaFin = ff,
            HoraInicio = hi,
            HoraFin = hf,
            Tipo = tipo,
            Motivo = (Motivo ?? "").Trim(),
            IdAdmin = User.GetId()
        });
        _db.SaveChanges();
        TempData["Ok"] = "Fecha sin servicio registrada.";
        return RedirectToPage();
    }

    public IActionResult OnPostEliminar(int id)
    {
        var f = _db.FechasNoServicio.Find(id);
        if (f != null) { _db.FechasNoServicio.Remove(f); _db.SaveChanges(); TempData["Ok"] = "Eliminada."; }
        return RedirectToPage();
    }
}
