// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static ISignalRBuilder AddSignalR(this IServiceCollection services)
        {
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultHubLifetimeManager<>));
            services.AddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
            services.AddSingleton(typeof(HubEndPoint<>), typeof(HubEndPoint<>));
            services.AddSingleton<IConfigureOptions<SignalROptions>, SignalROptionsSetup>();
            services.AddSingleton<JsonNetInvocationAdapter>();
            services.AddSingleton<InvocationAdapterRegistry>();

            return new SignalRBuilder(services);
        }

        public static ISignalRBuilder AddSignalROptions(this ISignalRBuilder builder, Action<SignalROptions> configure)
        {
            builder.Services.Configure(configure);
            return builder;
        }
    }
}
