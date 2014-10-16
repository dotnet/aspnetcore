// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.RequestContainer
{
    public class RequestServicesContainer : IDisposable
    {
        public RequestServicesContainer(
            HttpContext context,
            IServiceScopeFactory scopeFactory,
            IContextAccessor<HttpContext> appContextAccessor,
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
            if (appContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(appContextAccessor));
            }

            AppContextAccessor = appContextAccessor;

            Context = context;
            PriorAppServices = context.ApplicationServices;
            PriorRequestServices = context.RequestServices;

            // Begin the scope
            Scope = scopeFactory.CreateScope();
            ScopeContextAccessor = Scope.ServiceProvider.GetRequiredService<IContextAccessor<HttpContext>>();

            Context.ApplicationServices = appServiceProvider;
            Context.RequestServices = Scope.ServiceProvider;

            PriorAppHttpContext = AppContextAccessor.SetValue(context);
            PriorScopeHttpContext = ScopeContextAccessor.SetValue(context);
        }

        private HttpContext Context { get; set; }
        private IServiceProvider PriorAppServices { get; set; }
        private IServiceProvider PriorRequestServices { get; set; }
        private HttpContext PriorAppHttpContext { get; set; }
        private HttpContext PriorScopeHttpContext { get; set; }
        private IServiceScope Scope { get; set; }
        private IContextAccessor<HttpContext> ScopeContextAccessor { get; set; }
        private IContextAccessor<HttpContext> AppContextAccessor { get; set; }

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
            var rootHttpContextAccessor = serviceProvider.GetRequiredService<IContextAccessor<HttpContext>>();
            var rootServiceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            rootHttpContextAccessor.SetContextSource(ContainerMiddleware.AccessRootHttpContext, ContainerMiddleware.ExchangeRootHttpContext);

            // Pre Scope setup
            var priorApplicationServices = serviceProvider;
            var priorRequestServices = serviceProvider;

            var appServiceProvider = rootServiceProvider;
            var appServiceScopeFactory = rootServiceScopeFactory;
            var appHttpContextAccessor = rootHttpContextAccessor;

            if (priorApplicationServices != null &&
                priorApplicationServices != appServiceProvider)
            {
                appServiceProvider = priorApplicationServices;
                appServiceScopeFactory = priorApplicationServices.GetRequiredService<IServiceScopeFactory>();
                appHttpContextAccessor = priorApplicationServices.GetRequiredService<IContextAccessor<HttpContext>>();
            }

            // Creates the scope and does the service swaps
            return new RequestServicesContainer(httpContext, appServiceScopeFactory, appHttpContextAccessor, appServiceProvider);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ScopeContextAccessor.SetValue(PriorScopeHttpContext);
                    AppContextAccessor.SetValue(PriorAppHttpContext);

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
                ScopeContextAccessor = null;
                AppContextAccessor = null;
                PriorAppHttpContext = null;
                PriorScopeHttpContext = null;

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