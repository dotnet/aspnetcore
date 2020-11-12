// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Internal;
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
                var entry = _controllerActionInvokerCache.CreateEntry(ca);

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
                        ValueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(_valueProviderFactories),
                        ModelState =
                        {
                            MaxAllowedErrors = _maxModelValidationErrors
                        }
                    };

                    var controller = entry.ControllerFactory(controllerContext);

                    try
                    {
                        Dictionary<string, object> arguments = null;

                        if (entry.ControllerBinderDelegate != null)
                        {
                            // Allocation :(
                            arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                            // Debug.Assert(cacheEntry.ControllerBinderDelegate != null);
                            await entry.ControllerBinderDelegate(controllerContext, controller, arguments);
                        }

                        var orderedArguments = PrepareArguments(arguments, entry.ObjectMethodExecutor);

                        var result = await entry.ActionMethodExecutor.Execute(_actionResultTypeMapper, entry.ObjectMethodExecutor, controller, orderedArguments);

                        await result.ExecuteResultAsync(actionContext);
                    }
                    finally
                    {
                        entry.ControllerReleaser?.Invoke(controllerContext, controller);
                    }
                };
            }

            return null;
        }

        private static object[] PrepareArguments(
            IDictionary<string, object> actionParameters,
            ObjectMethodExecutor actionMethodExecutor)
        {
            if (actionParameters is null)
            {
                return null;
            }

            var declaredParameterInfos = actionMethodExecutor.MethodParameters;
            var count = declaredParameterInfos.Length;
            if (count == 0)
            {
                return null;
            }

            // Allocation :(
            var arguments = new object[count];
            for (var index = 0; index < count; index++)
            {
                var parameterInfo = declaredParameterInfos[index];

                if (!actionParameters.TryGetValue(parameterInfo.Name, out var value))
                {
                    value = actionMethodExecutor.GetDefaultValueForParameter(index);
                }

                arguments[index] = value;
            }

            return arguments;
        }
    }
}

