// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.OpenIdConnect;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure OpenIdConnect authentication options
    /// </summary>
    public static class OpenIdConnectServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureOpenIdConnect(this IServiceCollection services, Action<OpenIdConnectAuthenticationOptions> configure)
        {
            return services.ConfigureOptions(configure);
        }
    }
}
