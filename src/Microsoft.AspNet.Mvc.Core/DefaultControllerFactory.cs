using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerFactory : IControllerFactory
    {
        private readonly ITypeActivator _activator;
        private readonly IServiceProvider _serviceProvider;

        public DefaultControllerFactory(IServiceProvider serviceProvider, ITypeActivator activator)
        {
            _serviceProvider = serviceProvider;
            _activator = activator;
        }

        public object CreateController(ActionContext actionContext)
        {
            var actionDescriptor = actionContext.ActionDescriptor as ReflectedActionDescriptor;
            if (actionDescriptor == null)
            {
                return null;
            }

            try
            {
                var controller = _activator.CreateInstance(_serviceProvider, actionDescriptor.ControllerDescriptor.ControllerTypeInfo.AsType());

                // TODO: How do we feed the controller with context (need DI improvements)
                InitializeController(controller, actionContext);

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

        private void InitializeController(object controller, ActionContext actionContext)
        {
            var controllerType = controller.GetType();

            foreach (var prop in controllerType.GetRuntimeProperties())
            {
                if(prop.Name == "ActionContext" && prop.PropertyType.GetTypeInfo().IsAssignableFrom(typeof(ActionContext).GetTypeInfo()))
                {
                    prop.SetValue(controller, actionContext);
                }
                else if (prop.Name == "ViewData" && prop.PropertyType.GetTypeInfo().IsAssignableFrom(typeof(ViewData<object>).GetTypeInfo()))
                {
                    prop.SetValue(controller, new ViewData<object>(_serviceProvider.GetService<IModelMetadataProvider>(), actionContext.ModelState));
                }
                else if (prop.Name == "Url" && prop.PropertyType.GetTypeInfo().IsAssignableFrom(typeof(IUrlHelper).GetTypeInfo()))
                {
                    var urlHelper = new UrlHelper(
                        actionContext.HttpContext,
                        actionContext.Router,
                        actionContext.RouteValues);

                    prop.SetValue(controller, urlHelper);
                }
            }

            var method = controllerType.GetRuntimeMethods().FirstOrDefault(m => m.Name.Equals("Initialize", StringComparison.OrdinalIgnoreCase));

            if (method == null)
            {
                return;
            }

            var args = method.GetParameters()
                             .Select(p => _serviceProvider.GetService(p.ParameterType)).ToArray();

            method.Invoke(controller, args);
        }
    }
}
