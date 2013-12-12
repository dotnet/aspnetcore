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

        private static void DoCallback(Action<Type, Type> callback)
        {
            callback(typeof(IControllerFactory), typeof(DefaultControllerFactory));
            callback(typeof(IActionInvokerFactory), typeof(ControllerActionInvokerFactory));
        }
    }
}
