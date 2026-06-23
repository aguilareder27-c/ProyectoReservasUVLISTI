using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ReservaEspacios.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return User.IsInRole("Admin")
                ? RedirectToPage("/Admin/Index")
                : RedirectToPage("/Reservaciones/Index");
        return Page();
    }
}
