using System;
#if NET45
using System.Runtime.Remoting.Messaging;
#endif
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;

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

        private HttpContext AccessRootHttpContext()
        {
#if NET45
            return CallContext.LogicalGetData(LogicalDataKey) as HttpContext;
#else
            throw new NotImplementedException()
#endif
        }

        private HttpContext ExchangeRootHttpContext(HttpContext httpContext)
        {
#if NET45
            var prior = CallContext.LogicalGetData(LogicalDataKey) as HttpContext;
            CallContext.LogicalSetData(LogicalDataKey, httpContext);
            return prior;
#else
            throw new NotImplementedException()
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

            using (var scope = appServiceScopeFactory.CreateScope())
            {
                var scopeServiceProvider = scope.ServiceProvider;
                var scopeHttpContextAccessor = scopeServiceProvider.GetService<IContextAccessor<HttpContext>>();

                httpContext.ApplicationServices = appServiceProvider;
                httpContext.RequestServices = scopeServiceProvider;

                var priorAppHttpContext = appHttpContextAccessor.ExchangeValue(httpContext);
                var priorScopeHttpContext = scopeHttpContextAccessor.ExchangeValue(httpContext);
                
                try
                {
                    await _next.Invoke(httpContext);
                }
                finally
                {
                    scopeHttpContextAccessor.ExchangeValue(priorScopeHttpContext);
                    appHttpContextAccessor.ExchangeValue(priorAppHttpContext);

                    httpContext.RequestServices = priorRequestServices;
                    httpContext.ApplicationServices = priorApplicationServices;
                }
            }
        }
    }
}
