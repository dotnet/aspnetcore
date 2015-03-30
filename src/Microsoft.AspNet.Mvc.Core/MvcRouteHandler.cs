// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    public class MvcRouteHandler : IRouter
    {
        private ILogger _logger;

        public VirtualPathData GetVirtualPath([NotNull] VirtualPathContext context)
        {
            // The contract of this method is to check that the values coming in from the route are valid;
            // that they match an existing action, setting IsBound = true if the values are OK.
            var actionSelector = context.Context.RequestServices.GetRequiredService<IActionSelector>();
            context.IsBound = actionSelector.HasValidAction(context);

            // We return null here because we're not responsible for generating the url, the route is.
            return null;
        }

        public async Task RouteAsync([NotNull] RouteContext context)
        {
            var services = context.HttpContext.RequestServices;

            // Verify if AddMvc was done before calling UseMvc
            // We use the MvcMarkerService to make sure if all the services were added.
            MvcServicesHelper.ThrowIfMvcNotRegistered(services);

            EnsureLogger(context.HttpContext);
            var actionSelector = services.GetRequiredService<IActionSelector>();
            var actionDescriptor = await actionSelector.SelectAsync(context);

            if (actionDescriptor == null)
            {
                _logger.LogVerbose("No actions matched the current request.");
                return;
            }

            // Replacing the route data allows any code running here to dirty the route values or data-tokens
            // without affecting something upstream.
            var oldRouteData = context.RouteData;
            var newRouteData = new RouteData(oldRouteData);

            if (actionDescriptor.RouteValueDefaults != null)
            {
                foreach (var kvp in actionDescriptor.RouteValueDefaults)
                {
                    if (!newRouteData.Values.ContainsKey(kvp.Key))
                    {
                        newRouteData.Values.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            try
            {
                context.RouteData = newRouteData;

                using (_logger.BeginScope("ActionId: {ActionId}", actionDescriptor.Id))
                {
                    _logger.LogVerbose("Executing action {ActionDisplayName}", actionDescriptor.DisplayName);

                    await InvokeActionAsync(context, actionDescriptor);
                    context.IsHandled = true;
                }
            }
            finally
            {
                if (!context.IsHandled)
                {
                    context.RouteData = oldRouteData;
                }
            }
        }

        private async Task InvokeActionAsync(RouteContext context, ActionDescriptor actionDescriptor)
        {
            var services = context.HttpContext.RequestServices;
            Debug.Assert(services != null);

            var actionContext = new ActionContext(context.HttpContext, context.RouteData, actionDescriptor);

            var optionsAccessor = services.GetRequiredService<IOptions<MvcOptions>>();
            actionContext.ModelState.MaxAllowedErrors = optionsAccessor.Options.MaxModelValidationErrors;

            var contextAccessor = services.GetRequiredService<IScopedInstance<ActionContext>>();
            contextAccessor.Value = actionContext;
            var invokerFactory = services.GetRequiredService<IActionInvokerFactory>();
            var invoker = invokerFactory.CreateInvoker(actionContext);
            if (invoker == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatActionInvokerFactory_CouldNotCreateInvoker(
                        actionDescriptor.DisplayName));
            }

            await invoker.InvokeAsync();
        }

        private void EnsureLogger(HttpContext context)
        {
            if (_logger == null)
            {
                var factory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                _logger = factory.CreateLogger<MvcRouteHandler>();
            }
        }
    }
}
