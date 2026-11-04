// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Used to setup defaults for all <see cref="CookieAuthenticationOptions"/>.
/// </summary>
public class PostConfigureCookieAuthenticationOptions : IPostConfigureOptions<CookieAuthenticationOptions>
{
    private readonly IDataProtectionProvider _dp;

    /// <summary>
    /// Initializes a new instance of <see cref="PostConfigureCookieAuthenticationOptions"/>.
    /// </summary>
    /// <param name="dataProtection">The <see cref="IDataProtectionProvider"/>.</param>
    public PostConfigureCookieAuthenticationOptions(IDataProtectionProvider dataProtection)
    {
        _dp = dataProtection;
    }

    /// <summary>
    /// Invoked to post configure a TOptions instance.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="options">The options instance to configure.</param>
    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        options.DataProtectionProvider ??= _dp;

        ArgumentNullException.ThrowIfNull(name);

        if (string.IsNullOrEmpty(options.Cookie.Name))
        {
            options.Cookie.Name = CookieAuthenticationDefaults.CookiePrefix + Uri.EscapeDataString(name);
        }
        if (options.TicketDataFormat == null)
        {
            // Note: the purpose for the data protector must remain fixed for interop to work.
            var dataProtector = options.DataProtectionProvider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", name, "v2");
            options.TicketDataFormat = new TicketDataFormat(dataProtector);
        }
        if (options.CookieManager == null)
        {
            options.CookieManager = new ChunkingCookieManager();
        }
        if (!options.LoginPath.HasValue)
        {
            options.LoginPath = CookieAuthenticationDefaults.LoginPath;
        }
        if (!options.LogoutPath.HasValue)
        {
            options.LogoutPath = CookieAuthenticationDefaults.LogoutPath;
        }
        if (!options.AccessDeniedPath.HasValue)
        {
            options.AccessDeniedPath = CookieAuthenticationDefaults.AccessDeniedPath;
        }
    }
}
