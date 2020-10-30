// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class ControllerRequestDelegateFactory : IRequestDelegateFactory
    {
        private readonly ControllerActionInvokerCache _controllerActionInvokerCache;
        private readonly IActionResultTypeMapper _actionResultTypeMapper;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
        private readonly int _maxModelValidationErrors;

        public ControllerRequestDelegateFactory(
            ControllerActionInvokerCache controllerActionInvokerCache,
            IActionResultTypeMapper actionResultTypeMapper,
            IOptions<MvcOptions> options)
        {
            _controllerActionInvokerCache = controllerActionInvokerCache;
            _actionResultTypeMapper = actionResultTypeMapper;
            _valueProviderFactories = options.Value.ValueProviderFactories.ToArray();
            _maxModelValidationErrors = options.Value.MaxModelValidationErrors;
        }

        public RequestDelegate CreateRequestDelegate(ActionDescriptor actionDescriptor, RouteValueDictionary dataTokens)
        {
            // Super happy path (well assuming nobody cares about filters :O)
            if (actionDescriptor is ControllerActionDescriptor ca && ca.FilterDescriptors.Any(a => a.Filter is IApiBehaviorMetadata) && dataTokens == null)
            {
                return async context =>
                {
                    // Allocation :(
                    var routeData = new RouteData(context.Request.RouteValues);

                    // Allocation :(
                    var actionContext = new ActionContext(context, routeData, actionDescriptor);

                    // Allocation :(
                    var controllerContext = new ControllerContext(actionContext)
                    {
                        // Allocation :(
                        // PERF: These are rarely going to be changed, so let's go copy-on-write.
                        ValueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(_valueProviderFactories)
                    };

                    controllerContext.ModelState.MaxAllowedErrors = _maxModelValidationErrors;

                    var (cacheEntry, filters) = _controllerActionInvokerCache.GetCachedResult(controllerContext);

                    var controller = cacheEntry.ControllerFactory(controllerContext);

                    if (cacheEntry.ControllerBinderDelegate != null)
                    {
                        await cacheEntry.ControllerBinderDelegate(controllerContext, controller, null);
                    }

                    try
                    {
                        var result = await cacheEntry.ActionMethodExecutor.Execute(_actionResultTypeMapper, cacheEntry.ObjectMethodExecutor, controller, null);

                        await result.ExecuteResultAsync(actionContext);
                    }
                    finally
                    {
                        cacheEntry.ControllerReleaser?.Invoke(controllerContext, controller);
                    }
                };
            }

            return null;
        }
    }
}

