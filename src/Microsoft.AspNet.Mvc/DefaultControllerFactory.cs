using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerFactory : IControllerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultControllerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object CreateController(HttpContext context, string controllerName)
        {
            if (!controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                controllerName += "Controller";
            }

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = a.GetType(controllerName) ??
                               a.GetType(a.GetName().Name + "." + controllerName);
#if NET45
                    type = type ?? a.GetTypes().FirstOrDefault(t => t.Name.Equals(controllerName, StringComparison.OrdinalIgnoreCase));
#endif

                    if (type != null)
                    {
                        return ActivatorUtilities.CreateInstance(_serviceProvider, type);
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // TODO: Trace here 
                }
                catch (Exception)
                {
                    // TODO: Trace here
                }
            }

            return null;
        }

        public void ReleaseController(object controller)
        {

        }
    }
}
