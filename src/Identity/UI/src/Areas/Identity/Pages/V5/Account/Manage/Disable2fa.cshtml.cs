// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity.UI.V5.Pages.Account.Manage.Internal;

/// <summary>
///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
///     directly from your code. This API may change or be removed in future releases.
/// </summary>
[IdentityDefaultUI(typeof(Disable2faModel<>))]
public abstract class Disable2faModel : PageModel
{
    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual Task<IActionResult> OnGet() => throw new NotImplementedException();

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();
}

internal sealed class Disable2faModel<TUser> : Disable2faModel where TUser : class
{
    private readonly UserManager<TUser> _userManager;
    private readonly ILogger<Disable2faModel> _logger;

    public Disable2faModel(
        UserManager<TUser> userManager,
        ILogger<Disable2faModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public override async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            throw new InvalidOperationException($"Cannot disable 2FA for user as it's not currently enabled.");
        }

        return Page();
    }

    public override async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disable2faResult.Succeeded)
        {
            throw new InvalidOperationException($"Unexpected error occurred disabling 2FA.");
        }

        _logger.LogInformation(LoggerEventIds.TwoFADisabled, "User has disabled 2fa.");
        StatusMessage = "2fa has been disabled. You can reenable 2fa when you setup an authenticator app";
        return RedirectToPage("./TwoFactorAuthentication");
    }
}
