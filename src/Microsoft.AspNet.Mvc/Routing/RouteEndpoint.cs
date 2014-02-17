
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
        private IActionSelector _actionSelector;

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

        private IActionSelector ActionSelector
        {
            get
            {
                if (_actionSelector == null)
                {
                    _actionSelector = _services.GetService<IActionSelector>();
                }

                return _actionSelector;
            }
        }

        public async Task<bool> Send(HttpContext context)
        {
            var routeValues = context.GetFeature<IRouteValues>();
            var requestContext = new RequestContext(context, routeValues.Values);

            var actionDescriptor = ActionSelector.Select(requestContext);

            if (actionDescriptor == null)
            {
                return false;
            }

            var invoker = ActionInvokerFactory.CreateInvoker(new ActionContext(context, routeValues.Values, actionDescriptor));

            if (invoker == null)
            {
                var ex = new InvalidOperationException("Could not instantiate invoker for the actionDescriptor");

                // Add tracing/logging (what do we think of this pattern of tacking on extra data on the exception?)
                ex.Data.Add("AD", actionDescriptor);

                throw ex;
            }

            await invoker.InvokeActionAsync();

            return true;
        }
    }
}
