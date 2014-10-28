// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    public class MvcRouteHandler : IRouter
    {
        private ILogger _logger;

        public string GetVirtualPath([NotNull] VirtualPathContext context)
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

            Contract.Assert(services != null);

            // TODO: Throw an error here that's descriptive enough so that
            // users understand they should call the per request scoped middleware
            // or set HttpContext.Services manually

            EnsureLogger(context.HttpContext);
            using (_logger.BeginScope("MvcRouteHandler.RouteAsync"))
            {
                var actionSelector = services.GetRequiredService<IActionSelector>();
                var actionDescriptor = await actionSelector.SelectAsync(context);

                if (actionDescriptor == null)
                {
                    if (_logger.IsEnabled(TraceType.Verbose))
                    {
                        _logger.WriteValues(new MvcRouteHandlerRouteAsyncValues()
                        {
                            ActionSelected = false,
                            ActionInvoked = false,
                            Handled = context.IsHandled
                        });
                    }

                    return;
                }

                if (actionDescriptor.RouteValueDefaults != null)
                {
                    foreach (var kvp in actionDescriptor.RouteValueDefaults)
                    {
                        if (!context.RouteData.Values.ContainsKey(kvp.Key))
                        {
                            context.RouteData.Values.Add(kvp.Key, kvp.Value);
                        }
                    }
                }

                var actionContext = new ActionContext(context.HttpContext, context.RouteData, actionDescriptor);

                var optionsAccessor = services.GetRequiredService<IOptions<MvcOptions>>();
                actionContext.ModelState.MaxAllowedErrors = optionsAccessor.Options.MaxModelValidationErrors;

                var contextAccessor = services.GetRequiredService<IContextAccessor<ActionContext>>();
                using (contextAccessor.SetContextSource(() => actionContext, PreventExchange))
                {
                    var invokerFactory = services.GetRequiredService<IActionInvokerFactory>();
                    var invoker = invokerFactory.CreateInvoker(actionContext);
                    if (invoker == null)
                    {
                        var ex = new InvalidOperationException(
                            Resources.FormatActionInvokerFactory_CouldNotCreateInvoker(
                                actionDescriptor.DisplayName));

                        // Add tracing/logging (what do we think of this pattern of 
                        // tacking on extra data on the exception?)
                        ex.Data.Add("AD", actionDescriptor);

                        if (_logger.IsEnabled(TraceType.Verbose))
                        {
                            _logger.WriteValues(new MvcRouteHandlerRouteAsyncValues()
                            {
                                ActionSelected = true,
                                ActionInvoked = false,
                                Handled = context.IsHandled
                            });
                        }

                        throw ex;
                    }

                    await invoker.InvokeAsync();
                    context.IsHandled = true;

                    if (_logger.IsEnabled(TraceType.Verbose))
                    {
                        _logger.WriteValues(new MvcRouteHandlerRouteAsyncValues()
                        {
                            ActionSelected = true,
                            ActionInvoked = true,
                            Handled = context.IsHandled
                        });
                    }
                }
            }
        }

        private ActionContext PreventExchange(ActionContext contex)
        {
            throw new InvalidOperationException(Resources.ActionContextAccessor_SetValueNotSupported);
        }

        private void EnsureLogger(HttpContext context)
        {
            if (_logger == null)
            {
                var factory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                _logger = factory.Create<MvcRouteHandler>();
            }
        }
    }
}
