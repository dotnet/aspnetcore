
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc.Routing
{
    public class RouteEndpoint : IRouteEndpoint
    {
        private readonly IServiceProvider _services;
        private IActionInvokerFactory _actionInvokerFactory;

        // Using service provider here to prevent ordering issues with configuration...
        // IE: creating routes before configuring services, vice-versa.
        public RouteEndpoint(IServiceProvider services)
        {
            _services = services;
        }

        private IActionInvokerFactory ActionInvokerFactory
        {
            get
            {
                if (_actionInvokerFactory == null)
                {
                    _actionInvokerFactory = _services.GetService<IActionInvokerFactory>();
                }

                return _actionInvokerFactory;
            }
        }

        public async Task<bool> Send(HttpContext context)
        {
            var routeValues = context.GetFeature<IRouteValues>();
            var requestContext = new RequestContext(context, routeValues.Values);

            var invoker = ActionInvokerFactory.CreateInvoker(requestContext);
            if (invoker == null)
            {
                return false;
            }
            else
            {
                await invoker.InvokeActionAsync();
                return true;
            }
        }
    }
}
