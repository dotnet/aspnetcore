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
        private readonly IControllerDescriptorProvider _controllerDescriptorProvider;

        public DefaultControllerFactory(IServiceProvider serviceProvider, IControllerDescriptorProvider controllerDescriptorProvider)
        {
            _serviceProvider = serviceProvider;
            _controllerDescriptorProvider = controllerDescriptorProvider;
        }

        public object CreateController(HttpContext context, string controllerName)
        {            
            var controllers = _controllerDescriptorProvider.GetControllers(controllerName);

            if (controllers != null)
            {
                try
                {
                    var descriptor = controllers.SingleOrDefault();

                    if (descriptor != null)
                    {
                        try
                        {
                            var controller = ActivatorUtilities.CreateInstance(_serviceProvider, descriptor.ControllerType);

                            // TODO: How do we feed the controller with context (need DI improvements)
                            var contextProperty = descriptor.ControllerType.GetRuntimeProperty("Context");

                            if (contextProperty != null)
                            {
                                contextProperty.SetMethod.Invoke(controller, new object[] { context });
                            }

                            return controller;
                        }
                        catch (ReflectionTypeLoadException)
                        {
                        }
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
