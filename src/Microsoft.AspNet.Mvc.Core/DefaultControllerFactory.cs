using System;
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
            Injector.InjectProperty(controller, "ActionContext", actionContext);

            var viewData = new ViewDataDictionary<object>(
                _serviceProvider.GetService<IModelMetadataProvider>(),
                actionContext.ModelState);
            Injector.InjectProperty(controller, "ViewData", viewData);

            var urlHelper = new UrlHelper(
                actionContext.HttpContext,
                actionContext.Router,
                actionContext.RouteValues);
            Injector.InjectProperty(controller, "Url", urlHelper);

            Injector.CallInitializer(controller, _serviceProvider);
        }
    }
}
