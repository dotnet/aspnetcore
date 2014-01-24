using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc
{
    public class MvcHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public MvcHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task ExecuteAsync(HttpContext context, IRouteData routeData)
        {
            var requestContext = new RequestContext(context, routeData);

            IActionInvokerFactory invokerFactory = _serviceProvider.GetService<IActionInvokerFactory>();

            var invoker = invokerFactory.CreateInvoker(requestContext);

            return invoker.InvokeActionAsync();
        }
    }
}
