using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;

namespace ReservaEspacios.Pages.Admin;

public class UsuariosModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ReservaService _reservas;
    private readonly InasistenciaService _inasistencias;
    public UsuariosModel(AppDbContext db, ReservaService reservas, InasistenciaService inasistencias)
    { _db = db; _reservas = reservas; _inasistencias = inasistencias; }

    public class Fila
    {
        public Usuario Usuario { get; set; } = null!;
        public int Faltas { get; set; }
        public bool Sugerencia { get; set; }
    }

    public List<Fila> Filas { get; set; } = new();
    public int Umbral => InasistenciaService.Umbral;
    public int Ventana => InasistenciaService.DiasVentana;

    public void OnGet()
    {
        var usuarios = _db.Usuarios.OrderBy(u => u.Nombre).ToList();
        Filas = usuarios.Select(u => new Fila
        {
            Usuario = u,
            Faltas = _inasistencias.FaltasUltimos30(u.IdUsuario),
            Sugerencia = _inasistencias.SugerirBloqueo(u.IdUsuario)
        }).ToList();
    }

    public IActionResult OnPostBloquear(int id, string motivo)
    {
        var r = _reservas.CambiarEstadoUsuario(id, User.GetId(), true, motivo ?? "");
        TempData[r.Exito ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToPage();
    }

    public IActionResult OnPostDesbloquear(int id, string motivo)
    {
        var r = _reservas.CambiarEstadoUsuario(id, User.GetId(), false, motivo ?? "");
        TempData[r.Exito ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToPage();
    }
}
