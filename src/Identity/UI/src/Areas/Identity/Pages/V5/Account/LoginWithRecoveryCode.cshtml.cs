// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity.UI.V5.Pages.Account.Internal;

/// <summary>
///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
///     directly from your code. This API may change or be removed in future releases.
/// </summary>
[AllowAnonymous]
[IdentityDefaultUI(typeof(LoginWithRecoveryCodeModel<>))]
public abstract class LoginWithRecoveryCodeModel : PageModel
{
    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Recovery Code")]
        public string RecoveryCode { get; set; } = default!;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual Task<IActionResult> OnGetAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? returnUrl = null) => throw new NotImplementedException();

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual Task<IActionResult> OnPostAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? returnUrl = null) => throw new NotImplementedException();
}

internal sealed class LoginWithRecoveryCodeModel<TUser> : LoginWithRecoveryCodeModel where TUser : class
{
    private readonly SignInManager<TUser> _signInManager;
    private readonly UserManager<TUser> _userManager;
    private readonly ILogger<LoginWithRecoveryCodeModel> _logger;

    public LoginWithRecoveryCodeModel(
        SignInManager<TUser> signInManager,
        UserManager<TUser> userManager,
        ILogger<LoginWithRecoveryCodeModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    public override async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        // Ensure the user has gone through the username & password screen first
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            throw new InvalidOperationException($"Unable to load two-factor authentication user.");
        }

        ReturnUrl = returnUrl;

        return Page();
    }

    public override async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            throw new InvalidOperationException($"Unable to load two-factor authentication user.");
        }

        var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);

        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        await _userManager.GetUserIdAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation(LoggerEventIds.UserLoginWithRecoveryCode, "User logged in with a recovery code.");
            return LocalRedirect(returnUrl ?? Url.Content("~/"));
        }
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out.");
            return RedirectToPage("./Lockout");
        }
        else
        {
            _logger.LogWarning(LoggerEventIds.InvalidRecoveryCode, "Invalid recovery code entered.");
            ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
            return Page();
        }
    }
}
