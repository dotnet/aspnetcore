using System;
using System.Threading.Tasks;
using Microsoft.AspNet.CoreServices;
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
            string[] parts = (context.Request.PathBase + context.Request.Path).Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // {controller}/{action}
            string controllerName = GetPartOrDefault(parts, 0, "HomeController");
            string actionName = GetPartOrDefault(parts, 1, "Index");

            var factory = _serviceProvider.GetService<IControllerFactory>();
            object controller = factory.CreateController(context, controllerName);

            if (controller == null)
            {
                throw new InvalidOperationException(String.Format("Couldn't find controller '{0}'.", controllerName));
            }

            var controllerBase = controller as Controller;

            if (controllerBase != null)
            {
                // TODO: Make this the controller context
                controllerBase.Initialize(context);
            }

            var controllerContext = new ControllerContext(context, controller);

            IActionInvokerFactory invokerFactory = _serviceProvider.GetService<IActionInvokerFactory>();
            var invoker = invokerFactory.CreateInvoker(controllerContext);

            return invoker.InvokeActionAsync(actionName);
        }

        private static string GetPartOrDefault(string[] parts, int index, string defaultValue)
        {
            return index < parts.Length ? parts[index] : defaultValue;
        }
    }
}
