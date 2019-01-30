// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RoutingWebSite
{
    public class StartupWithoutEndpointRouting : Startup
    {
        // Do not call base implementations of these methods. Those are specific to endpoint routing.
        protected override void ConfigureMvcOptions(MvcOptions options)
        {
            options.EnableEndpointRouting = false;
        }

        protected override void ConfigureRoutingServices(IServiceCollection services)
        {
            // EndpointRoutingController is not compatible with old routing
            // Remove its action to avoid errors
            var actionDescriptorProvider = new RemoveControllerActionDescriptorProvider(
                new ControllerToRemove
                {
                    ControllerType = typeof(EndpointRoutingController),
                    Actions = null, // remove all
                },
                new ControllerToRemove
                {
                    ControllerType = typeof(PageRouteController),
                    Actions = new[] { nameof(PageRouteController.AttributeRoute) }
                });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IActionDescriptorProvider>(actionDescriptorProvider));
        }

        protected override void ConfigureConventionalTransformerRoute(IRouteBuilder routes)
        {
            // no-op
        }

        protected override void ConfigurePageRoute(IRouteBuilder routes)
        {
            // no-op
        }
    }
}
