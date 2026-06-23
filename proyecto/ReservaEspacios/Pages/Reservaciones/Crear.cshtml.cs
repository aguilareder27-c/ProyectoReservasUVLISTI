using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;

namespace ReservaEspacios.Pages.Reservaciones;

public class CrearModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ReservaService _reservas;
    public CrearModel(AppDbContext db, ReservaService reservas) { _db = db; _reservas = reservas; }

    public List<SelectListItem> EspaciosOpts { get; set; } = new();

    [BindProperty] public int IdEspacio { get; set; }
    [BindProperty] public string TipoEvento { get; set; } = "Normal";
    [BindProperty] public string? Fecha { get; set; }         // Normal (yyyy-MM-dd)
    [BindProperty] public string? FechaInicio { get; set; }   // UV
    [BindProperty] public string? FechaFin { get; set; }      // UV
    [BindProperty] public string? HoraInicio { get; set; }    // HH:mm
    [BindProperty] public string? HoraFin { get; set; }
    [BindProperty] public string Motivo { get; set; } = "";

    public string? Error { get; set; }

    private void Cargar()
    {
        EspaciosOpts = _db.Espacios
            .Where(e => e.Estado == EstadoEspacio.Disponible)
            .OrderBy(e => e.Nombre)
            .Select(e => new SelectListItem { Value = e.IdEspacio.ToString(), Text = e.Nombre })
            .ToList();
    }

    public void OnGet(int? idEspacio)
    {
        Cargar();
        if (idEspacio.HasValue) IdEspacio = idEspacio.Value;
    }

    public IActionResult OnPost()
    {
        Cargar();
        int idUsuario = User.GetId();

        if (!Fmt.TryTime(HoraInicio, out var hi) || !Fmt.TryTime(HoraFin, out var hf))
        {
            Error = "Indica una hora de inicio y de fin válidas.";
            return Page();
        }
        if (string.IsNullOrWhiteSpace(Motivo))
        {
            Error = "Indica el motivo de la reservación.";
            return Page();
        }

        ResultadoOperacion r;
        if (TipoEvento == "UV")
        {
            if (!Fmt.TryDate(FechaInicio, out var fi) || !Fmt.TryDate(FechaFin, out var ff))
            {
                Error = "Indica la fecha de inicio y de fin del evento UV.";
                return Page();
            }
            r = _reservas.CrearUV(idUsuario, IdEspacio, fi, ff, hi, hf, Motivo.Trim());
        }
        else
        {
            if (!Fmt.TryDate(Fecha, out var f))
            {
                Error = "Indica la fecha de la reservación.";
                return Page();
            }
            r = _reservas.CrearNormal(idUsuario, IdEspacio, f, hi, hf, Motivo.Trim());
        }

        if (!r.Exito)
        {
            Error = r.Mensaje;
            if (r.Detalles.Count > 0) Error += " " + string.Join(" ", r.Detalles);
            return Page();
        }

        TempData["Ok"] = r.Mensaje;
        if (r.Detalles.Count > 0) TempData["Detalles"] = string.Join("\n", r.Detalles);
        return RedirectToPage("/Reservaciones/Index");
    }
}
