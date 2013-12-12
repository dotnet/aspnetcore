using System;
using System.Linq;
using System.Collections.Generic;
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

            var controllerContext = new ControllerContext(context, controller);

            Initialize(controller, controllerContext);

            IActionInvokerFactory invokerFactory = _serviceProvider.GetService<IActionInvokerFactory>();
            var invoker = invokerFactory.CreateInvoker(controllerContext);

            return invoker.InvokeActionAsync(actionName);
        }

        private void Initialize(object controller, ControllerContext controllerContext)
        {
            var controllerType = controller.GetType();

            foreach (var prop in controllerType.GetProperties())
            {
                if (prop.Name == "Context")
                {
                    if (prop.PropertyType == typeof(IOwinContext))
                    {
                        prop.SetValue(controller, controllerContext.HttpContext);
                    }
                    else if (prop.PropertyType == typeof(IDictionary<string, object>))
                    {
                        prop.SetValue(controller, controllerContext.HttpContext.Environment);
                    }
                }
            }

            var method = controllerType.GetMethod("Initialize");

            if (method == null)
            {
                return;
            }

            var args = method.GetParameters()
                             .Select(p => _serviceProvider.GetService(p.ParameterType)).ToArray();

            method.Invoke(controller, args);
        }

        private static string GetPartOrDefault(string[] parts, int index, string defaultValue)
        {
            return index < parts.Length ? parts[index] : defaultValue;
        }
    }
}
