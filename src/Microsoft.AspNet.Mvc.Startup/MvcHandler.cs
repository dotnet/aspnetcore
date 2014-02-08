using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class MvcHandler
    {
        private readonly IActionInvokerFactory _actionInvokerFactory;

        public MvcHandler(IActionInvokerFactory actionInvokerFactory)
        {
            _actionInvokerFactory = actionInvokerFactory;
        }

        public Task ExecuteAsync(HttpContext context, IRouteData routeData)
        {
            var requestContext = new RequestContext(context, routeData);

            var invoker = _actionInvokerFactory.CreateInvoker(requestContext);

            return invoker.InvokeActionAsync();
        }
    }
}
