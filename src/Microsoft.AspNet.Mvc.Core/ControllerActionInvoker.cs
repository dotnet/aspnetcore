// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionInvoker : FilterActionInvoker
    {
        private readonly ControllerActionDescriptor _descriptor;
        private readonly IControllerFactory _controllerFactory;
        private readonly IInputFormattersProvider _inputFormattersProvider;
        private readonly IControllerActionArgumentBinder _actionInvocationProvider;

        public ControllerActionInvoker([NotNull] ActionContext actionContext,
                                      [NotNull] INestedProviderManager<FilterProviderContext> filterProvider,
                                      [NotNull] IControllerFactory controllerFactory,
                                      [NotNull] ControllerActionDescriptor descriptor,
                                      [NotNull] IInputFormattersProvider inputFormattersProvider,
                                      [NotNull] IControllerActionArgumentBinder controllerActionArgumentBinder)
            : base(actionContext, filterProvider)
        {
            _descriptor = descriptor;
            _controllerFactory = controllerFactory;
            _inputFormattersProvider = inputFormattersProvider;
            _actionInvocationProvider = controllerActionArgumentBinder;
            if (descriptor.MethodInfo == null)
            {
                throw new ArgumentException(
                    Resources.FormatPropertyOfTypeCannotBeNull("MethodInfo",
                                                               typeof(ControllerActionDescriptor)),
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
            var actionReturnValue = await ControllerActionExecutor.ExecuteAsync(
                actionMethodInfo,
                ActionContext.Controller,
                actionExecutingContext.ActionArguments);

            var actionResult = CreateActionResult(
                actionMethodInfo.ReturnType,
                actionReturnValue);
            return actionResult;
        }

        protected override Task<IDictionary<string, object>> GetActionArgumentsAsync(ActionContext context)
        {
            return _actionInvocationProvider.GetActionArgumentsAsync(context);
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
