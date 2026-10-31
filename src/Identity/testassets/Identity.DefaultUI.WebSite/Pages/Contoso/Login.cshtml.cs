// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Identity.DefaultUI.WebSite.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    public LoginModel(IOptionsMonitor<ContosoAuthenticationOptions> options)
    {
        Options = options.CurrentValue;
    }

    public class InputModel
    {
        [Required]
        public string Login { get; set; }
        public bool RememberMe { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; }

    [BindProperty]
    public string State { get; set; }

    public ContosoAuthenticationOptions Options { get; }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        else
        {
            var state = JsonConvert.DeserializeObject<IDictionary<string, string>>(State);
            var identity = new ClaimsIdentity(new Claim[]
            {
                    new Claim(ClaimTypes.NameIdentifier, Input.Login)
            },
            state["LoginProvider"],
            ClaimTypes.NameIdentifier,
            ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);
            var properties = new AuthenticationProperties(state)
            {
                IsPersistent = Input.RememberMe
            };
            await HttpContext.SignInAsync(Options.SignInScheme, principal, properties);
            return Redirect(ReturnUrl ?? "~/");
        }
    }
}
