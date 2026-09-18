// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal;

/// <summary>
///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
///     directly from your code. This API may change or be removed in future releases.
/// </summary>
[IdentityDefaultUI(typeof(SetPasswordModel<>))]
public abstract class SetPasswordModel : PageModel
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
    [TempData]
    public string? StatusMessage { get; set; }

    // Internal until #69376, the SetPassword reauthentication API proposal, is approved.
    internal bool IsReauthenticated { get; set; }

    // Internal until #69376, the SetPassword reauthentication API proposal, is approved.
    internal IList<UserLoginInfo>? CurrentLogins { get; set; }

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
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = default!;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual Task<IActionResult> OnGetAsync() => throw new NotImplementedException();

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual Task<IActionResult> OnPostReauthenticateAsync(string provider) => throw new NotImplementedException();

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual Task<IActionResult> OnGetReauthenticationCallbackAsync() => throw new NotImplementedException();
}

internal sealed class SetPasswordModel<TUser> : SetPasswordModel where TUser : class
{
    private readonly UserManager<TUser> _userManager;
    private readonly SignInManager<TUser> _signInManager;

    public SetPasswordModel(
        UserManager<TUser> userManager,
        SignInManager<TUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public override async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);

        if (hasPassword)
        {
            return RedirectToPage("./ChangePassword");
        }

        await LoadAsync(user);
        return Page();
    }

    public override async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        if (CurrentLogins is null)
        {
            ModelState.AddModelError(string.Empty, "Setting a password requires a user store that supports security stamps.");
        }
        else if (!IsReauthenticated)
        {
            ModelState.AddModelError(string.Empty, "You must confirm your identity before setting a password.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var addPasswordResult = await _userManager.AddPasswordAsync(user, Input.NewPassword);
        if (!addPasswordResult.Succeeded)
        {
            foreach (var error in addPasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync(user);
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        ReauthenticationMarker.Clear(HttpContext);
        StatusMessage = "Your password has been set.";

        return RedirectToPage();
    }

    public override async Task<IActionResult> OnPostReauthenticateAsync(string provider)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        if (CurrentLogins is null)
        {
            ModelState.AddModelError(string.Empty, "Setting a password requires a user store that supports security stamps.");
            return Page();
        }

        if (string.IsNullOrEmpty(provider) || !CurrentLogins.Any(login => login.LoginProvider == provider))
        {
            ModelState.AddModelError(string.Empty, "The selected external login is not available for confirmation.");
            return Page();
        }

        ReauthenticationMarker.Clear(HttpContext);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        var redirectUrl = Url.Page("./SetPassword", pageHandler: "ReauthenticationCallback");
        var userId = await _userManager.GetUserIdAsync(user);
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, userId);
        return new ChallengeResult(provider, properties);
    }

    public override async Task<IActionResult> OnGetReauthenticationCallbackAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var info = await _signInManager.GetExternalLoginInfoAsync(userId);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        if (info is null)
        {
            StatusMessage = "Error: Could not load external login info.";
            return RedirectToPage();
        }

        var linkedUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (linkedUser is null || !string.Equals(await _userManager.GetUserIdAsync(linkedUser), userId, StringComparison.Ordinal))
        {
            StatusMessage = "Error: That login is not linked to this account.";
            return RedirectToPage();
        }

        if (!_userManager.SupportsUserSecurityStamp)
        {
            StatusMessage = "Error: Setting a password requires a user store that supports security stamps.";
            return RedirectToPage();
        }

        await ReauthenticationMarker.MarkAsync(HttpContext, _userManager, user);
        return RedirectToPage();
    }

    private async Task LoadAsync(TUser user)
    {
        IsReauthenticated = await ReauthenticationMarker.IsVerifiedAsync(HttpContext, _userManager, user);
        if (!_userManager.SupportsUserSecurityStamp)
        {
            CurrentLogins = null;
            return;
        }

        if (!_userManager.SupportsUserLogin)
        {
            CurrentLogins = [];
            return;
        }

        var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        var logins = await _userManager.GetLoginsAsync(user);
        CurrentLogins = logins.Where(login => schemes.Any(scheme => scheme.Name == login.LoginProvider)).ToList();
    }
}
