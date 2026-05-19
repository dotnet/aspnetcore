// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite;

public class CustomModelTypeModel : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; }

    public string ReturnUrl { get; set; }

    [BindRequired]
    [FromQuery(Name = nameof(Attempts))]
    public int Attempts { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public virtual void OnGet(string returnUrl = null)
    {
        throw new NotImplementedException();
    }

    public virtual IActionResult OnPostAsync(string returnUrl = null)
    {
        throw new NotImplementedException();
    }
}

public class User
{
}

internal class CustomModelTypeModel<TUser> : CustomModelTypeModel where TUser : User
{
    private readonly ILogger<CustomModelTypeModel<TUser>> _logger;

    public CustomModelTypeModel(ILogger<CustomModelTypeModel<TUser>> logger)
    {
        _logger = logger;
    }

    public override void OnGet(string returnUrl = null)
    {
        // We only care about being able to resolve the service from DI.
        // The line below is just to make the compiler happy.
        _logger.LogInformation(typeof(TUser).Name);
        ViewData["UserType"] = typeof(TUser).Name;
        ReturnUrl = returnUrl;
    }

    public override IActionResult OnPostAsync(string returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            Attempts++;
            RouteData.Values.Add(nameof(Attempts), Attempts);

            return Page();
        }

        return Redirect("~/");
    }
}
