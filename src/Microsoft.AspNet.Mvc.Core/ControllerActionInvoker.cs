// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionInvoker : FilterActionInvoker
    {
        private readonly ControllerActionDescriptor _descriptor;
        private readonly IControllerFactory _controllerFactory;
        private readonly IControllerActionArgumentBinder _argumentBinder;

        public ControllerActionInvoker(
            [NotNull] ActionContext actionContext,
            [NotNull] INestedProviderManager<FilterProviderContext> filterProvider,
            [NotNull] IControllerFactory controllerFactory,
            [NotNull] ControllerActionDescriptor descriptor,
            [NotNull] IInputFormattersProvider inputFormatterProvider,
            [NotNull] IControllerActionArgumentBinder controllerActionArgumentBinder,
            [NotNull] IModelBinderProvider modelBinderProvider,
            [NotNull] IModelValidatorProviderProvider modelValidatorProviderProvider,
            [NotNull] IValueProviderFactoryProvider valueProviderFactoryProvider,
            [NotNull] IScopedInstance<ActionBindingContext> actionBindingContextAccessor)
            : base(
                  actionContext, 
                  filterProvider,
                  inputFormatterProvider, 
                  modelBinderProvider, 
                  modelValidatorProviderProvider, 
                  valueProviderFactoryProvider,
                  actionBindingContextAccessor)
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

        protected override Task<IDictionary<string, object>> GetActionArgumentsAsync(
            ActionContext context, 
            ActionBindingContext bindingContext)
        {
            return _argumentBinder.GetActionArgumentsAsync(context, bindingContext);
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
                return new ObjectResult(null)
                {
                    // Treat the declared type as void, which is the unwrapped type for Task.
                    DeclaredType = typeof(void)
                };
            }

            // Unwrap potential Task<T> types.
            var actualReturnType = TypeHelper.GetTaskInnerTypeOrNull(declaredReturnType) ?? declaredReturnType;
            if (actionReturnValue == null && typeof(IActionResult).IsAssignableFrom(actualReturnType))
            {
                throw new InvalidOperationException(
                    Resources.FormatActionResult_ActionReturnValueCannotBeNull(actualReturnType));
            }

            return new ObjectResult(actionReturnValue)
            {
                DeclaredType = actualReturnType
            };
        }
    }
}
