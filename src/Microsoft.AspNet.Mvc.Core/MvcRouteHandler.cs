
using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class MvcRouteHandler : IRouter
    {
        public string GetVirtualPath([NotNull] VirtualPathContext context)
        {
            // The contract of this method is to check that the values coming in from the route are valid;
            // that they match an existing action, setting IsBound = true if the values are OK.
            var actionSelector = context.Context.RequestServices.GetService<IActionSelector>();
            context.IsBound = actionSelector.HasValidAction(context);

            // We return null here because we're not responsible for generating the url, the route is.
            return null;
        }

        public async Task RouteAsync([NotNull] RouteContext context)
        {
            var services = context.HttpContext.RequestServices;
            Contract.Assert(services != null);

            // TODO: Throw an error here that's descriptive enough so that
            // users understand they should call the per request scoped middleware
            // or set HttpContext.Services manually

            var requestContext = new RequestContext(context.HttpContext, context.Values);

            var actionSelector = services.GetService<IActionSelector>();
            var actionDescriptor = await actionSelector.SelectAsync(requestContext);
            if (actionDescriptor == null)
            {
                return;
            }

            var actionContext = new ActionContext(context.HttpContext, context.Router, context.Values, actionDescriptor);

            var contextAccessor = services.GetService<IContextAccessor<ActionContext>>();
            using (contextAccessor.SetContextSource(() => actionContext, PreventExchange))
            {
                var invokerFactory = services.GetService<IActionInvokerFactory>();
                var invoker = invokerFactory.CreateInvoker(actionContext);
                if (invoker == null)
                {
                    var ex = new InvalidOperationException(
                        Resources.FormatActionInvokerFactory_CouldNotCreateInvoker(actionDescriptor));

                    // Add tracing/logging (what do we think of this pattern of tacking on extra data on the exception?)
                    ex.Data.Add("AD", actionDescriptor);

                    throw ex;
                }

                await invoker.InvokeActionAsync();

                context.IsHandled = true;
            }
        }

        private ActionContext PreventExchange(ActionContext contex)
        {
            throw new InvalidOperationException(Resources.ActionContextAccessor_SetValueNotSupported);
        }
    }
}
