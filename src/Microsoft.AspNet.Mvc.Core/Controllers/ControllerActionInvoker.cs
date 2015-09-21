// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Controllers
{
    public class ControllerActionInvoker : FilterActionInvoker
    {
        private readonly ControllerActionDescriptor _descriptor;
        private readonly IControllerFactory _controllerFactory;
        private readonly IControllerActionArgumentBinder _argumentBinder;

        public ControllerActionInvoker(
            [NotNull] ActionContext actionContext,
            [NotNull] IReadOnlyList<IFilterProvider> filterProviders,
            [NotNull] IControllerFactory controllerFactory,
            [NotNull] ControllerActionDescriptor descriptor,
            [NotNull] IReadOnlyList<IInputFormatter> inputFormatters,
            [NotNull] IReadOnlyList<IOutputFormatter> outputFormatters,
            [NotNull] IControllerActionArgumentBinder controllerActionArgumentBinder,
            [NotNull] IReadOnlyList<IModelBinder> modelBinders,
            [NotNull] IReadOnlyList<IModelValidatorProvider> modelValidatorProviders,
            [NotNull] IReadOnlyList<IValueProviderFactory> valueProviderFactories,
            [NotNull] IActionBindingContextAccessor actionBindingContextAccessor,
            [NotNull] ILogger logger,
            [NotNull] TelemetrySource telemetry,
            int maxModelValidationErrors)
            : base(
                  actionContext,
                  filterProviders,
                  inputFormatters,
                  outputFormatters,
                  modelBinders,
                  modelValidatorProviders,
                  valueProviderFactories,
                  actionBindingContextAccessor,
                  logger,
                  telemetry,
                  maxModelValidationErrors)
        {
            _descriptor = descriptor;
            _controllerFactory = controllerFactory;
            _argumentBinder = controllerActionArgumentBinder;

            if (descriptor.MethodInfo == null)
            {
                throw new ArgumentException(
                    Resources.FormatPropertyOfTypeCannotBeNull("MethodInfo",
                                                               typeof(ControllerActionDescriptor)),
                    "descriptor");
            }
        }

        protected override object CreateInstance()
        {
            // The binding context is used in activation
            Debug.Assert(ActionBindingContext != null);
            return _controllerFactory.CreateController(ActionContext);
        }

        protected override void ReleaseInstance(object instance)
        {
            _controllerFactory.ReleaseController(instance);
        }

        protected override async Task<IActionResult> InvokeActionAsync(ActionExecutingContext actionExecutingContext)
        {
            var actionMethodInfo = _descriptor.MethodInfo;
            var actionReturnValue = await ControllerActionExecutor.ExecuteAsync(
                actionMethodInfo,
                actionExecutingContext.Controller,
                actionExecutingContext.ActionArguments);

            var actionResult = CreateActionResult(
                actionMethodInfo.ReturnType,
                actionReturnValue);
            return actionResult;
        }

        protected override Task<IDictionary<string, object>> BindActionArgumentsAsync(
            ActionContext context,
            ActionBindingContext bindingContext)
        {
            return _argumentBinder.BindActionArgumentsAsync(context, bindingContext, Instance);
        }

        // Marking as internal for Unit Testing purposes.
        internal static IActionResult CreateActionResult([NotNull] Type declaredReturnType, object actionReturnValue)
        {
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
                typeof(IActionResult).GetTypeInfo().IsAssignableFrom(actualReturnType.GetTypeInfo()))
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
