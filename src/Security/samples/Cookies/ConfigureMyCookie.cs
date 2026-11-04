// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace AuthSamples.Cookies;

internal class ConfigureMyCookie : IConfigureNamedOptions<CookieAuthenticationOptions>
{
    // You can inject services here
    public ConfigureMyCookie()
    {
    }

    public void Configure(string name, CookieAuthenticationOptions options)
    {
        // Only configure the schemes you want
        if (name == Startup.CookieScheme)
        {
            // options.LoginPath = "/someotherpath";
        }
    }

    public void Configure(CookieAuthenticationOptions options)
        => Configure(Options.DefaultName, options);
}
