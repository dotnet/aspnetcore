// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal;

/// <summary>
///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
///     directly from your code. This API may change or be removed in future releases.
/// </summary>
[IdentityDefaultUI(typeof(DownloadPersonalDataModel<>))]
public abstract class DownloadPersonalDataModel : PageModel
{
    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual IActionResult OnGet() => throw new NotImplementedException();

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();
}

internal sealed class DownloadPersonalDataModel<TUser> : DownloadPersonalDataModel where TUser : class
{
    private readonly UserManager<TUser> _userManager;
    private readonly ILogger<DownloadPersonalDataModel> _logger;

    public DownloadPersonalDataModel(
        UserManager<TUser> userManager,
        ILogger<DownloadPersonalDataModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public override IActionResult OnGet()
    {
        return NotFound();
    }

    public override async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        _logger.LogInformation(LoggerEventIds.PersonalDataRequested, "User asked for their personal data.");

        // Only include personal data for download
        var personalData = new Dictionary<string, string?>();
        var personalDataProps = typeof(TUser).GetProperties().Where(
                        prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
        foreach (var p in personalDataProps)
        {
            personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
        }

        var logins = await _userManager.GetLoginsAsync(user);
        foreach (var l in logins)
        {
            personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
        }

        personalData.Add($"Authenticator Key", await _userManager.GetAuthenticatorKeyAsync(user));

        Response.Headers.Add("Content-Disposition", "attachment; filename=PersonalData.json");
        return new FileContentResult(JsonSerializer.SerializeToUtf8Bytes(personalData), "application/json");
    }
}
