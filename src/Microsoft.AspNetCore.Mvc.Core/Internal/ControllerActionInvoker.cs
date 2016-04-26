// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
#if NETSTANDARD1_5
using System.Reflection;
#endif
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvoker : FilterActionInvoker
    {
        private readonly ControllerActionDescriptor _descriptor;
        private readonly IControllerFactory _controllerFactory;
        private readonly IControllerArgumentBinder _argumentBinder;

        public ControllerActionInvoker(
            ActionContext actionContext,
            ControllerActionInvokerCache controllerActionInvokerCache,
            IControllerFactory controllerFactory,
            ControllerActionDescriptor descriptor,
            IControllerArgumentBinder argumentBinder,
            IReadOnlyList<IValueProviderFactory> valueProviderFactories,
            ILogger logger,
            DiagnosticSource diagnosticSource,
            int maxModelValidationErrors)
            : base(
                  actionContext,
                  controllerActionInvokerCache,
                  valueProviderFactories,
                  logger,
                  diagnosticSource,
                  maxModelValidationErrors)
        {
            if (controllerFactory == null)
            {
                throw new ArgumentNullException(nameof(controllerFactory));
            }

            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (argumentBinder == null)
            {
                throw new ArgumentNullException(nameof(argumentBinder));
            }

            _controllerFactory = controllerFactory;
            _descriptor = descriptor;
            _argumentBinder = argumentBinder;

            if (descriptor.MethodInfo == null)
            {
                throw new ArgumentException(
                    Resources.FormatPropertyOfTypeCannotBeNull(
                        nameof(descriptor.MethodInfo),
                        typeof(ControllerActionDescriptor)),
                    nameof(descriptor));
            }
        }

        protected override object CreateInstance()
        {
            return _controllerFactory.CreateController(Context);
        }

        protected override void ReleaseInstance(object instance)
        {
            _controllerFactory.ReleaseController(Context, instance);
        }

        protected override async Task<IActionResult> InvokeActionAsync(ActionExecutingContext actionExecutingContext)
        {
            if (actionExecutingContext == null)
            {
                throw new ArgumentNullException(nameof(actionExecutingContext));
            }

            var actionMethodInfo = _descriptor.MethodInfo;

            var methodExecutor = GetControllerActionMethodExecutor();

            var arguments = ControllerActionExecutor.PrepareArguments(
                actionExecutingContext.ActionArguments,
                actionMethodInfo.GetParameters());

            Logger.ActionMethodExecuting(actionExecutingContext, arguments);

            var actionReturnValue = await ControllerActionExecutor.ExecuteAsync(
                methodExecutor,
                actionExecutingContext.Controller,
                arguments);

            var actionResult = CreateActionResult(
                actionMethodInfo.ReturnType,
                actionReturnValue);

            Logger.ActionMethodExecuted(actionExecutingContext, actionResult);

            return actionResult;
        }

        protected override Task BindActionArgumentsAsync(IDictionary<string, object> arguments)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            return _argumentBinder.BindArgumentsAsync(Context, Instance, arguments);
        }

        // Marking as internal for Unit Testing purposes.
        internal static IActionResult CreateActionResult(Type declaredReturnType, object actionReturnValue)
        {
            if (declaredReturnType == null)
            {
                throw new ArgumentNullException(nameof(declaredReturnType));
            }

            // optimize common path
            var actionResult = actionReturnValue as IActionResult;
            if (actionResult != null)
            {
                return actionResult;
            }

            if (declaredReturnType == typeof(void) ||
                declaredReturnType == typeof(Task))
            {
                return new EmptyResult();
            }

            // Unwrap potential Task<T> types.
            var actualReturnType = GetTaskInnerTypeOrNull(declaredReturnType) ?? declaredReturnType;
            if (actionReturnValue == null &&
                typeof(IActionResult).IsAssignableFrom(actualReturnType))
            {
                throw new InvalidOperationException(
                    Resources.FormatActionResult_ActionReturnValueCannotBeNull(actualReturnType));
            }

            return new ObjectResult(actionReturnValue)
            {
                DeclaredType = actualReturnType
            };
        }

        private static Type GetTaskInnerTypeOrNull(Type type)
        {
            var genericType = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(Task<>));

            return genericType?.GenericTypeArguments[0];
        }
    }
}
