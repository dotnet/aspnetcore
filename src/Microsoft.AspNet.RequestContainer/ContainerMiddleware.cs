// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if ASPNET50
using System.Runtime.Remoting.Messaging;
#endif
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
#if ASPNET50
using System.Runtime.Remoting;
#endif
namespace Microsoft.AspNet.RequestContainer
{
    public class ContainerMiddleware
    {
        private const string LogicalDataKey = "__HttpContext_Current__";
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _rootServiceProvider;
        private readonly IContextAccessor<HttpContext> _rootHttpContextAccessor;
        private readonly IServiceScopeFactory _rootServiceScopeFactory;

        public ContainerMiddleware(
            RequestDelegate next,
            IServiceProvider rootServiceProvider,
            IContextAccessor<HttpContext> rootHttpContextAccessor,
            IServiceScopeFactory rootServiceScopeFactory)
        {
            if (rootServiceProvider == null)
            {
                throw new ArgumentNullException("rootServiceProvider");
            }
            if (rootHttpContextAccessor == null)
            {
                throw new ArgumentNullException("rootHttpContextAccessor");
            }
            if (rootServiceScopeFactory == null)
            {
                throw new ArgumentNullException("rootServiceScopeFactory");
            }

            _next = next;
            _rootServiceProvider = rootServiceProvider;
            _rootServiceScopeFactory = rootServiceScopeFactory;
            _rootHttpContextAccessor = rootHttpContextAccessor;

            _rootHttpContextAccessor.SetContextSource(AccessRootHttpContext, ExchangeRootHttpContext);
        }

        internal static HttpContext AccessRootHttpContext()
        {
#if ASPNET50
            var handle = CallContext.LogicalGetData(LogicalDataKey) as ObjectHandle;
            return handle != null ? handle.Unwrap() as HttpContext : null;
#else
            throw new Exception("TODO: CallContext not available");
#endif 
        }

        internal static HttpContext ExchangeRootHttpContext(HttpContext httpContext)
        {
#if ASPNET50
            var prior = CallContext.LogicalGetData(LogicalDataKey) as ObjectHandle;
            CallContext.LogicalSetData(LogicalDataKey, new ObjectHandle(httpContext));
            return prior != null ? prior.Unwrap() as HttpContext : null;
#else
            return null;
#endif
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.RequestServices != null)
            {
                throw new Exception("TODO: nested request container scope? this is probably a mistake on your part?");
            }

            var priorApplicationServices = httpContext.ApplicationServices;
            var priorRequestServices = httpContext.RequestServices;

            var appServiceProvider = _rootServiceProvider;
            var appServiceScopeFactory = _rootServiceScopeFactory;
            var appHttpContextAccessor = _rootHttpContextAccessor;

            if (priorApplicationServices != null &&
                priorApplicationServices != appServiceProvider)
            {
                appServiceProvider = priorApplicationServices;
                appServiceScopeFactory = priorApplicationServices.GetService<IServiceScopeFactory>();
                appHttpContextAccessor = priorApplicationServices.GetService<IContextAccessor<HttpContext>>();
            }

            using (var container = new RequestServicesContainer(httpContext, appServiceScopeFactory, appHttpContextAccessor, appServiceProvider))
            {
                await _next.Invoke(httpContext);
            }
        }
    }
}
