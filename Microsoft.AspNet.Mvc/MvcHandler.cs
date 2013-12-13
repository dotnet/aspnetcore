using System;
using System.Threading.Tasks;
using Microsoft.AspNet.CoreServices;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public class MvcHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public MvcHandler()
            : this(null)
        {
        }

        public MvcHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? MvcServices.Create();
        }

        public Task ExecuteAsync(IOwinContext context)
        {
            var routeData = new FakeRouteData(context);

            IActionInvokerFactory invokerFactory = _serviceProvider.GetService<IActionInvokerFactory>();
            var invoker = invokerFactory.CreateInvoker(new RequestContext(context, routeData));

            return invoker.InvokeActionAsync();
        }
    }
}
