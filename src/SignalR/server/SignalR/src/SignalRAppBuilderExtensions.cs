// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class SignalRAppBuilderExtensions
    {
        /// <summary>
        /// Adds SignalR to the <see cref="IApplicationBuilder"/> request execution pipeline.
        /// <para>
        ///     This method is obsolete and will be removed in a future version.
        ///     The recommended alternative is to use MapHub&#60;THub&#62; inside Microsoft.AspNetCore.Builder.UseEndpoints(...).
        /// </para>
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="configure">A callback to configure hub routes.</param>
        /// <returns>The same instance of the <see cref="IApplicationBuilder"/> for chaining.</returns>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is to use MapHub<THub> inside Microsoft.AspNetCore.Builder.UseEndpoints(...).")]
        public static IApplicationBuilder UseSignalR(this IApplicationBuilder app, Action<HubRouteBuilder> configure)
        {
            var marker = app.ApplicationServices.GetService<SignalRMarkerService>();
            if (marker == null)
            {
                throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling " +
                                                    "'IServiceCollection.AddSignalR' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }

            app.UseWebSockets();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                configure(new HubRouteBuilder(endpoints));
            });

            return app;
        }
    }
}
