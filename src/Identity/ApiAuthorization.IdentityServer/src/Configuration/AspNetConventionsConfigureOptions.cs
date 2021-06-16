// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    internal class AspNetConventionsConfigureOptions : IConfigureOptions<IdentityServerOptions>
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
}
