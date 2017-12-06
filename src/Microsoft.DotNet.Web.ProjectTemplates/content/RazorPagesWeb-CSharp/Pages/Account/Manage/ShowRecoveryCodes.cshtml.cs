using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Company.WebApplication1.Pages.Account.Manage
{
    public class ShowRecoveryCodesModel : PageModel
    {
        public string[] RecoveryCodes { get; private set; }

        public IActionResult OnGet()
        {
            RecoveryCodes = (string[])TempData["RecoveryCodes"];
            if (RecoveryCodes == null)
            {
                return RedirectToPage("TwoFactorAuthentication");
            }

            return Page();
        }
    }
}