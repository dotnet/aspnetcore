// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Negotiate.Internal;

internal sealed class NegotiateOptionsValidationStartupFilter : IStartupFilter
{
    private readonly string _authenticationScheme;

    public NegotiateOptionsValidationStartupFilter(string authenticationScheme)
    {
        _authenticationScheme = authenticationScheme;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            // Resolve NegotiateOptions on startup to trigger post configuration and bind LdapConnection if needed
            var options = builder.ApplicationServices.GetRequiredService<IOptionsMonitor<NegotiateOptions>>().Get(_authenticationScheme);
            next(builder);
        };
    }
}
