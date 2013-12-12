using System;
using Microsoft.AspNet.CoreServices;

namespace Microsoft.AspNet.Mvc
{
    public static class MvcServices
    {
        public static IServiceProvider Create()
        {
            var services = new ServiceProvider();
            DoCallback((service, implementation) => services.Add(service, implementation));
            return services;
        }

        public static void DoCallback(Action<Type, Type> callback)
        {
            callback(typeof(IControllerFactory), typeof(DefaultControllerFactory));
            callback(typeof(IActionInvokerFactory), typeof(ControllerActionInvokerFactory));
            callback(typeof(IActionResultHelper), typeof(ActionResultHelper));
            callback(typeof(IActionResultFactory), typeof(ActionResultFactory));
        }
    }
}
