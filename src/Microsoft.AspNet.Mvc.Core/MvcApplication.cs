
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class MvcApplication : IRouter
    {
        private readonly IServiceProvider _services;
        private IActionInvokerFactory _actionInvokerFactory;
        private IActionSelector _actionSelector;

        // Using service provider here to prevent ordering issues with configuration...
        // IE: creating routes before configuring services, vice-versa.
        public MvcApplication(IServiceProvider services)
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

        public string BindPath(BindPathContext context)
        {
            // For now just allow any values to target this application.
            context.IsBound = true;
            return null;
        }

        public async Task RouteAsync(RouteContext context)
        {
            var requestContext = new RequestContext(context.HttpContext, context.Values);

            var actionDescriptor = await ActionSelector.SelectAsync(requestContext);
            if (actionDescriptor == null)
            {
                return;
            }

            var actionContext = new ActionContext(context.HttpContext, context.Values, actionDescriptor);
            var invoker = ActionInvokerFactory.CreateInvoker(actionContext);
            if (invoker == null)
            {
                var ex = new InvalidOperationException("Could not instantiate invoker for the actionDescriptor");

                // Add tracing/logging (what do we think of this pattern of tacking on extra data on the exception?)
                ex.Data.Add("AD", actionDescriptor);

                throw ex;
            }

            await invoker.InvokeActionAsync();

            context.IsHandled = true;
        }
    }
}
