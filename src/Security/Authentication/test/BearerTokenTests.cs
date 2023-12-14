// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication.BearerToken;

public class BearerTokenTests : SharedAuthenticationTests<BearerTokenOptions>
{
    protected override string DefaultScheme => BearerTokenDefaults.AuthenticationScheme;

    protected override Type HandlerType
    {
        get
        {
            var services = new ServiceCollection();
            services.AddAuthentication().AddBearerToken();
            return services.Select(d => d.ServiceType).Single(typeof(AuthenticationHandler<BearerTokenOptions>).IsAssignableFrom);
        }
    }

    protected override void RegisterAuth(AuthenticationBuilder services, Action<BearerTokenOptions> configure)
    {
        services.AddBearerToken(configure);
    }
}
