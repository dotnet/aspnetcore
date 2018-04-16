// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR
{
    public static class SignalRConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseHub<THub>(this IConnectionBuilder connectionBuilder) where THub : Hub
        {
            var marker = connectionBuilder.ApplicationServices.GetService(typeof(SignalRMarkerService));
            if (marker == null)
            {
                throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling " +
                    "'IServiceCollection.AddSignalR' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }

            return connectionBuilder.UseConnectionHandler<HubConnectionHandler<THub>>();
        }
    }
}
