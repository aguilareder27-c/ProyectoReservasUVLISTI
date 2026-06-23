using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;

namespace ReservaEspacios.Pages.Admin.Espacios;

public class EditarModel : PageModel
{
    private readonly AppDbContext _db;
    public EditarModel(AppDbContext db) { _db = db; }

    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Nombre { get; set; } = "";
    [BindProperty] public string Tipo { get; set; } = "";
    [BindProperty] public int Capacidad { get; set; }
    [BindProperty] public string Ubicacion { get; set; } = "";
    [BindProperty] public string? Descripcion { get; set; }
    [BindProperty] public string EstadoStr { get; set; } = "Disponible";
    [BindProperty] public string? HoraApertura { get; set; }
    [BindProperty] public string? HoraCierre { get; set; }
    [BindProperty] public List<int> Dias { get; set; } = new(); // 1..7

    public string? Error { get; set; }
    public bool EsNuevo => Id == 0;

    public void OnGet(int? id)
    {
        if (id.HasValue)
        {
            var e = _db.Espacios.Include(x => x.DiasOperacion).FirstOrDefault(x => x.IdEspacio == id.Value);
            if (e != null)
            {
                Id = e.IdEspacio;
                Nombre = e.Nombre; Tipo = e.Tipo; Capacidad = e.Capacidad;
                Ubicacion = e.Ubicacion; Descripcion = e.Descripcion;
                EstadoStr = e.Estado.ToString();
                HoraApertura = Fmt.Hm(e.HoraApertura);
                HoraCierre = Fmt.Hm(e.HoraCierre);
                Dias = e.DiasOperacion.Select(d => (int)d.DiaSemana).ToList();
            }
        }
        else
        {
            HoraApertura = "07:00";
            HoraCierre = "21:00";
            Dias = new List<int> { 1, 2, 3, 4, 5 };
        }
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Nombre) || Capacidad <= 0
            || !Fmt.TryTime(HoraApertura, out var ha) || !Fmt.TryTime(HoraCierre, out var hc))
        {
            Error = "Completa nombre, capacidad y horario válidos.";
            return Page();
        }
        if (hc <= ha)
        {
            Error = "La hora de cierre debe ser mayor a la de apertura.";
            return Page();
        }

        Espacio e;
        if (Id == 0)
        {
            e = new Espacio();
            _db.Espacios.Add(e);
        }
        else
        {
            e = _db.Espacios.Include(x => x.DiasOperacion).First(x => x.IdEspacio == Id);
        }

        e.Nombre = Nombre.Trim();
        e.Tipo = Tipo?.Trim() ?? "";
        e.Capacidad = Capacidad;
        e.Ubicacion = Ubicacion?.Trim() ?? "";
        e.Descripcion = Descripcion?.Trim();
        e.Estado = EstadoStr == "Inactivo" ? EstadoEspacio.Inactivo : EstadoEspacio.Disponible;
        e.HoraApertura = ha;
        e.HoraCierre = hc;

        // Reemplaza los días de operación
        e.DiasOperacion.Clear();
        foreach (var d in Dias.Distinct())
            e.DiasOperacion.Add(new DiaOperacionEspacio { DiaSemana = (DiaSemana)d });

        _db.SaveChanges();
        TempData["Ok"] = "Espacio guardado.";
        return RedirectToPage("/Admin/Espacios/Index");
    }
}
