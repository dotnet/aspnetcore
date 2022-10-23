// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer;

internal sealed class AspNetConventionsConfigureOptions : IConfigureOptions<IdentityServerOptions>
{
    public void Configure(IdentityServerOptions options)
    {
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;
        options.Authentication.CookieAuthenticationScheme = IdentityConstants.ApplicationScheme;
        options.UserInteraction.ErrorUrl = "/Home";
    }
}
