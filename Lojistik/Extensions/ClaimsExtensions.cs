using System.Security.Claims;

namespace Lojistik.Extensions  // klasör adına göre namespace
{
    public static class ClaimsExtensions
    {
        public static int GetFirmaId(this ClaimsPrincipal user)
            => int.TryParse(user.FindFirst("FirmaID")?.Value, out var id) ? id : 0;

        public static string? GetFirmaKodu(this ClaimsPrincipal user)
            => user.FindFirst("FirmaKodu")?.Value;

        public static int GetUserId(this ClaimsPrincipal user)
            => int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    }
}
