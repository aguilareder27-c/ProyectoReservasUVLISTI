using System.Globalization;
using System.Security.Claims;

namespace ReservaEspacios.Services;

public static class ClaimsExtensions
{
    public static int GetId(this ClaimsPrincipal u)
        => int.TryParse(u.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    public static string GetNombre(this ClaimsPrincipal u)
        => u.FindFirst(ClaimTypes.Name)?.Value ?? "";
}

public static class Fmt
{
    public static bool TryDate(string? s, out DateOnly d)
        => DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d);

    public static bool TryTime(string? s, out TimeOnly t)
        => TimeOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out t);

    public static string Hm(TimeOnly t) => t.ToString(@"HH\:mm");
    public static string Hm(TimeOnly? t) => t.HasValue ? t.Value.ToString(@"HH\:mm") : "—";
    public static string Dmy(DateOnly d) => d.ToString("dd/MM/yyyy");
}
