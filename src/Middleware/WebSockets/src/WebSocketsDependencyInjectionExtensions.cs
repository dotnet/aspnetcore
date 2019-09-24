// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.WebSockets
{
    public static class WebSocketsDependencyInjectionExtensions
    {
        public static IServiceCollection AddWebSockets(this IServiceCollection services, Action<WebSocketOptions> configure)
        {
            return services.Configure(configure);
        }
    }
}
