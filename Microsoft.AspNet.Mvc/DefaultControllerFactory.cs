using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.CoreServices;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerFactory : IControllerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultControllerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object CreateController(IOwinContext context, string controllerName)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = a.GetType(controllerName) ?? 
                           a.GetType(a.GetName().Name + "." + controllerName) ??
                    a.GetTypes().FirstOrDefault(t => t.Name.Equals(controllerName, StringComparison.OrdinalIgnoreCase));

                if (type != null)
                {
                    return ActivatorUtilities.CreateInstance(_serviceProvider, type);
                }
            }

            return null;
        }

        public void ReleaseController(object controller)
        {

        }
    }
}
