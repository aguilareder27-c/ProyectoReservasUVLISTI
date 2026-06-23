using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReservaEspacios.Data;
using ReservaEspacios.Services;

namespace ReservaEspacios.Pages.Account;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;
    public LoginModel(AppDbContext db) { _db = db; }

    [BindProperty] public string Correo { get; set; } = "";
    [BindProperty] public string Contrasena { get; set; } = "";
    public string? Error { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var correo = (Correo ?? "").Trim().ToLower();

        var admin = _db.Administradores.FirstOrDefault(a => a.Correo.ToLower() == correo);
        if (admin != null && PasswordHasher.Verify(Contrasena, admin.Contrasena))
        {
            await SignIn(admin.IdAdmin, admin.Nombre, "Admin");
            return RedirectToPage("/Admin/Index");
        }

        var u = _db.Usuarios.FirstOrDefault(x => x.Correo.ToLower() == correo);
        if (u != null && PasswordHasher.Verify(Contrasena, u.Contrasena))
        {
            await SignIn(u.IdUsuario, u.Nombre, "Usuario");
            return RedirectToPage("/Reservaciones/Index");
        }

        Error = "Correo o contraseña incorrectos.";
        return Page();
    }

    private async Task SignIn(int id, string nombre, string rol)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString()),
            new(ClaimTypes.Name, nombre),
            new(ClaimTypes.Role, rol)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
