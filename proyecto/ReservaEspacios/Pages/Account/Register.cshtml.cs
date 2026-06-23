using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReservaEspacios.Data;
using ReservaEspacios.Models;
using ReservaEspacios.Services;

namespace ReservaEspacios.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly AppDbContext _db;
    public RegisterModel(AppDbContext db) { _db = db; }

    [BindProperty] public string Nombre { get; set; } = "";
    [BindProperty] public string Correo { get; set; } = "";
    [BindProperty] public string Contrasena { get; set; } = "";
    public string? Error { get; set; }

    public void OnGet() { }

    public IActionResult OnPost()
    {
        var correo = (Correo ?? "").Trim().ToLower();
        if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(correo) || (Contrasena?.Length ?? 0) < 6)
        {
            Error = "Completa todos los campos (la contraseña debe tener al menos 6 caracteres).";
            return Page();
        }
        if (_db.Usuarios.Any(u => u.Correo.ToLower() == correo) ||
            _db.Administradores.Any(a => a.Correo.ToLower() == correo))
        {
            Error = "Ese correo ya está registrado.";
            return Page();
        }

        _db.Usuarios.Add(new Usuario
        {
            Nombre = Nombre.Trim(),
            Correo = correo,
            Contrasena = PasswordHasher.Hash(Contrasena!),
            Estado = EstadoUsuario.Activo
        });
        _db.SaveChanges();

        TempData["Ok"] = "Cuenta creada. Ya puedes iniciar sesión.";
        return RedirectToPage("/Account/Login");
    }
}
