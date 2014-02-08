using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerFactory : IControllerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ControllerCache _controllerCache;

        public DefaultControllerFactory(IServiceProvider serviceProvider, ControllerCache cache)
        {
            _serviceProvider = serviceProvider;
            _controllerCache = cache;
        }

        public object CreateController(HttpContext context, string controllerName)
        {
            if (!controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                controllerName += "Controller";
            }

            var controllers = _controllerCache.GetController(controllerName);

            if (controllers != null)
            {
                try
                {
                    var type = controllers.Single().ControllerType;

                    try
                    {
                        return ActivatorUtilities.CreateInstance(_serviceProvider, type);
                    }
                    catch (ReflectionTypeLoadException)
                    {
                    }
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidOperationException("Ambiguity: Duplicate controllers match the controller name");
                }
            }

            return null;
        }

        public void ReleaseController(object controller)
        {

        }
    }
}
