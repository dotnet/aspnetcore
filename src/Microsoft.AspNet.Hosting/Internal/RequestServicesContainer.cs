// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class RequestServicesContainer : IDisposable
    {
        public RequestServicesContainer(
            HttpContext context,
            IServiceScopeFactory scopeFactory,
            IServiceProvider appServiceProvider)
        {
            if (scopeFactory == null)
            {
                throw new ArgumentNullException(nameof(scopeFactory));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Context = context;
            PriorAppServices = context.ApplicationServices;
            PriorRequestServices = context.RequestServices;

            // Begin the scope
            Scope = scopeFactory.CreateScope();

            Context.ApplicationServices = appServiceProvider;
            Context.RequestServices = Scope.ServiceProvider;
        }

        private HttpContext Context { get; set; }
        private IServiceProvider PriorAppServices { get; set; }
        private IServiceProvider PriorRequestServices { get; set; }
        private IServiceScope Scope { get; set; }

        // CONSIDER: this could be an extension method on HttpContext instead
        public static RequestServicesContainer EnsureRequestServices(HttpContext httpContext, IServiceProvider services)
        {
            // All done if we already have a request services
            if (httpContext.RequestServices != null)
            {
                return null;
            }

            var serviceProvider = httpContext.ApplicationServices ?? services;
            if (serviceProvider == null)
            {
                throw new InvalidOperationException("TODO: services and httpContext.ApplicationServices are both null!");
            }

            // Matches constructor of RequestContainer
            var rootServiceProvider = serviceProvider.GetRequiredService<IServiceProvider>();
            var rootServiceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            // Pre Scope setup
            var priorApplicationServices = serviceProvider;
            var priorRequestServices = serviceProvider;

            var appServiceProvider = rootServiceProvider;
            var appServiceScopeFactory = rootServiceScopeFactory;

            if (priorApplicationServices != null &&
                priorApplicationServices != appServiceProvider)
            {
                appServiceProvider = priorApplicationServices;
                appServiceScopeFactory = priorApplicationServices.GetRequiredService<IServiceScopeFactory>();
            }

            // Creates the scope and does the service swaps
            return new RequestServicesContainer(httpContext, appServiceScopeFactory, appServiceProvider);
        }

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Context.RequestServices = PriorRequestServices;
                    Context.ApplicationServices = PriorAppServices;
                }

                if (Scope != null)
                {
                    Scope.Dispose();
                    Scope = null;
                }

                Context = null;
                PriorAppServices = null;
                PriorRequestServices = null;

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
#endregion
    }
}