using System;
using Microsoft.AspNet.CoreServices;
using System.Net.Http.Formatting;

namespace Microsoft.AspNet.Mvc
{
    public static class MvcServices
    {
        public static ServiceProvider Create()
        {
            var services = new ServiceProvider();
            DoCallback((service, implementation) => services.Add(service, implementation));
            return services;
        }

        public static void DoCallback(Action<Type, Type> callback)
        {
            callback(typeof(IControllerFactory), typeof(DefaultControllerFactory));
            callback(typeof(IActionInvokerFactory), typeof(ActionInvokerFactory));
            callback(typeof(IActionResultHelper), typeof(ActionResultHelper));
            callback(typeof(IActionResultFactory), typeof(ActionResultFactory));
            callback(typeof(IContentNegotiator), typeof(DefaultContentNegotiator));


            // TODO: Should be many
            callback(typeof(IActionDescriptorProvider), typeof(ActionDescriptorProvider));
            callback(typeof(IActionInvokerProvider), typeof(ActionInvokerProvider));
        }
    }
}
