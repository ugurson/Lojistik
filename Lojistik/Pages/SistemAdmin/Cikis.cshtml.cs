using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lojistik.Pages.SistemAdmin;

public class CikisModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync("SistemAdminScheme");
        return RedirectToPage("/SistemAdmin/Giris");
    }
}
