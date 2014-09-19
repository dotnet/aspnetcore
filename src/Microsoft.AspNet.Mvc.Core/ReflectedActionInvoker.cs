// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class ReflectedActionInvoker : FilterActionInvoker
    {
        private readonly ReflectedActionDescriptor _descriptor;
        private readonly IControllerFactory _controllerFactory;
        private readonly IInputFormattersProvider _inputFormattersProvider;

        public ReflectedActionInvoker([NotNull] ActionContext actionContext,
                                      [NotNull] IActionBindingContextProvider bindingContextProvider,
                                      [NotNull] INestedProviderManager<FilterProviderContext> filterProvider,
                                      [NotNull] IControllerFactory controllerFactory,
                                      [NotNull] ReflectedActionDescriptor descriptor,
                                      [NotNull] IInputFormattersProvider inputFormattersProvider)
            : base(actionContext, bindingContextProvider, filterProvider)
        {
            _descriptor = descriptor;
            _controllerFactory = controllerFactory;
            _inputFormattersProvider = inputFormattersProvider;
            if (descriptor.MethodInfo == null)
            {
                throw new ArgumentException(
                    Resources.FormatPropertyOfTypeCannotBeNull("MethodInfo",
                                                               typeof(ReflectedActionDescriptor)),
                    "descriptor");
            }
        }

        public async override Task InvokeAsync()
        {
            var controller = _controllerFactory.CreateController(ActionContext);
            try
            {
                ActionContext.Controller = controller;
                ActionContext.InputFormatters = _inputFormattersProvider.InputFormatters
                                                                        .ToList();
                await base.InvokeAsync();
            }
            finally
            {
                _controllerFactory.ReleaseController(ActionContext.Controller);
            }
        }
            
        protected override async Task<IActionResult> InvokeActionAsync(ActionExecutingContext actionExecutingContext)
        {
            var actionMethodInfo = _descriptor.MethodInfo;
            var actionReturnValue = await ReflectedActionExecutor.ExecuteAsync(
                actionMethodInfo,
                ActionContext.Controller,
                actionExecutingContext.ActionArguments);

            var actionResult = CreateActionResult(
                actionMethodInfo.ReturnType,
                actionReturnValue);
            return actionResult;
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
