using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Owin;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class MvcHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public MvcHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task ExecuteAsync(IOwinContext context, IRouteData routeData)
        {
            var requestContext = new RequestContext(context, routeData);

            IActionInvokerFactory invokerFactory = _serviceProvider.GetService<IActionInvokerFactory>();

            var invoker = invokerFactory.CreateInvoker(requestContext);

            return invoker.InvokeActionAsync();
        }
    }
}
