using System;
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

        public object CreateController(HttpContext context, ActionDescriptor actionDescriptor)
        {
            var typedAd = actionDescriptor as ReflectedActionDescriptor;

            if (typedAd == null)
            {
                return null;
            }

            try
            {
                var controller = ActivatorUtilities.CreateInstance(_serviceProvider, typedAd.ControllerDescriptor.ControllerTypeInfo.AsType());

                // TODO: How do we feed the controller with context (need DI improvements)
                var contextProperty = controller.GetType().GetRuntimeProperty("Context");

                if (contextProperty != null)
                {
                    contextProperty.SetMethod.Invoke(controller, new object[] { context });
                }

                return controller;
            }
            catch (ReflectionTypeLoadException)
            {
            }

            return null;
        }

        public void ReleaseController(object controller)
        {
        }
    }
}
