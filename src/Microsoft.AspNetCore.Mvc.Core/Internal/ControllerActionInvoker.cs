// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvoker : ResourceInvoker, IActionInvoker
    {
        private readonly ControllerActionInvokerCacheEntry _cacheEntry;
        private readonly ControllerContext _controllerContext;

        private Dictionary<string, object> _arguments;

        private ActionExecutingContext _actionExecutingContext;
        private ActionExecutedContext _actionExecutedContext;

        internal ControllerActionInvoker(
            ILogger logger,
            DiagnosticSource diagnosticSource,
            ControllerContext controllerContext,
            ControllerActionInvokerCacheEntry cacheEntry,
            IFilterMetadata[] filters)
            : base(diagnosticSource, logger, controllerContext, filters, controllerContext.ValueProviderFactories)
        {
            if (cacheEntry == null)
            {
                throw new ArgumentNullException(nameof(cacheEntry));
            }

            _cacheEntry = cacheEntry;
            _controllerContext = controllerContext;
        }

        // Internal for testing
        internal ControllerContext ControllerContext => _controllerContext;

        protected override void ReleaseResources()
        {
            if (_instance != null && _cacheEntry.ControllerReleaser != null)
            {
                _cacheEntry.ControllerReleaser(_controllerContext, _instance);
            }
        }

        private Task Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
        {
            switch (next)
            {
                case State.ActionBegin:
                    {
                        var controllerContext = _controllerContext;

                        _cursor.Reset();

                        _instance = _cacheEntry.ControllerFactory(controllerContext);

                        _arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                        var task = BindArgumentsAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ActionNext;
                            return task;
                        }

                        goto case State.ActionNext;
                    }

                case State.ActionNext:
                    {
                        var current = _cursor.GetNextFilter<IActionFilter, IAsyncActionFilter>();
                        if (current.FilterAsync != null)
                        {
                            if (_actionExecutingContext == null)
                            {
                                _actionExecutingContext = new ActionExecutingContext(_controllerContext, _filters, _arguments, _instance);
                            }

                            state = current.FilterAsync;
                            goto case State.ActionAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_actionExecutingContext == null)
                            {
                                _actionExecutingContext = new ActionExecutingContext(_controllerContext, _filters, _arguments, _instance);
                            }

                            state = current.Filter;
                            goto case State.ActionSyncBegin;
                        }
                        else
                        {
                            goto case State.ActionInside;
                        }
                    }

                case State.ActionAsyncBegin:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_actionExecutingContext != null);

                        var filter = (IAsyncActionFilter)state;
                        var actionExecutingContext = _actionExecutingContext;

                        _diagnosticSource.BeforeOnActionExecution(actionExecutingContext, filter);

                        var task = filter.OnActionExecutionAsync(actionExecutingContext, InvokeNextActionFilterAwaitedAsync);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ActionAsyncEnd;
                            return task;
                        }

                        goto case State.ActionAsyncEnd;
                    }

                case State.ActionAsyncEnd:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_actionExecutingContext != null);

                        var filter = (IAsyncActionFilter)state;

                        if (_actionExecutedContext == null)
                        {
                            // If we get here then the filter didn't call 'next' indicating a short circuit.
                            _logger.ActionFilterShortCircuited(filter);

                            _actionExecutedContext = new ActionExecutedContext(
                                _controllerContext,
                                _filters,
                                _instance)
                            {
                                Canceled = true,
                                Result = _actionExecutingContext.Result,
                            };
                        }

                        _diagnosticSource.AfterOnActionExecution(_actionExecutedContext, filter);

                        goto case State.ActionEnd;
                    }

                case State.ActionSyncBegin:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_actionExecutingContext != null);

                        var filter = (IActionFilter)state;
                        var actionExecutingContext = _actionExecutingContext;

                        _diagnosticSource.BeforeOnActionExecuting(actionExecutingContext, filter);

                        filter.OnActionExecuting(actionExecutingContext);

                        _diagnosticSource.AfterOnActionExecuting(actionExecutingContext, filter);

                        if (actionExecutingContext.Result != null)
                        {
                            // Short-circuited by setting a result.
                            _logger.ActionFilterShortCircuited(filter);

                            _actionExecutedContext = new ActionExecutedContext(
                                _actionExecutingContext,
                                _filters,
                                _instance)
                            {
                                Canceled = true,
                                Result = _actionExecutingContext.Result,
                            };

                            goto case State.ActionEnd;
                        }

                        var task = InvokeNextActionFilterAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ActionSyncEnd;
                            return task;
                        }

                        goto case State.ActionSyncEnd;
                    }

                case State.ActionSyncEnd:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_actionExecutingContext != null);
                        Debug.Assert(_actionExecutedContext != null);

                        var filter = (IActionFilter)state;
                        var actionExecutedContext = _actionExecutedContext;

                        _diagnosticSource.BeforeOnActionExecuted(actionExecutedContext, filter);

                        filter.OnActionExecuted(actionExecutedContext);

                        _diagnosticSource.AfterOnActionExecuted(actionExecutedContext, filter);

                        goto case State.ActionEnd;
                    }

                case State.ActionInside:
                    {
                        var task = InvokeActionMethodAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ActionEnd;
                            return task;
                        }

                        goto case State.ActionEnd;
                    }

                case State.ActionEnd:
                    {
                        if (scope == Scope.Action)
                        {
                            if (_actionExecutedContext == null)
                            {
                                _actionExecutedContext = new ActionExecutedContext(_controllerContext, _filters, _instance)
                                {
                                    Result = _result,
                                };
                            }

                            isCompleted = true;
                            return Task.CompletedTask;
                        }

                        var actionExecutedContext = _actionExecutedContext;
                        Rethrow(actionExecutedContext);

                        if (actionExecutedContext != null)
                        {
                            _result = actionExecutedContext.Result;
                        }

                        isCompleted = true;
                        return Task.CompletedTask;
                    }

                default:
                    throw new InvalidOperationException();
            }
        }

        private async Task InvokeNextActionFilterAsync()
        {
            try
            {
                var next = State.ActionNext;
                var state = (object)null;
                var scope = Scope.Action;
                var isCompleted = false;
                while (!isCompleted)
                {
                    await Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                _actionExecutedContext = new ActionExecutedContext(_controllerContext, _filters, _instance)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(_actionExecutedContext != null);
        }

        private async Task<ActionExecutedContext> InvokeNextActionFilterAwaitedAsync()
        {
            Debug.Assert(_actionExecutingContext != null);
            if (_actionExecutingContext.Result != null)
            {
                // If we get here, it means that an async filter set a result AND called next(). This is forbidden.
                var message = Resources.FormatAsyncActionFilter_InvalidShortCircuit(
                    typeof(IAsyncActionFilter).Name,
                    nameof(ActionExecutingContext.Result),
                    typeof(ActionExecutingContext).Name,
                    typeof(ActionExecutionDelegate).Name);

                throw new InvalidOperationException(message);
            }

            await InvokeNextActionFilterAsync();

            Debug.Assert(_actionExecutedContext != null);
            return _actionExecutedContext;
        }

        private async Task InvokeActionMethodAsync()
        {
            var controllerContext = _controllerContext;
            var objectMethodExecutor = _cacheEntry.ObjectMethodExecutor;
            var controller = _instance;
            var arguments = _arguments;
            var actionMethodExecutor = _cacheEntry.ActionMethodExecutor;
            var orderedArguments = PrepareArguments(arguments, objectMethodExecutor);

            var diagnosticSource = _diagnosticSource;
            var logger = _logger;

            IActionResult result = null;
            try
            {
                diagnosticSource.BeforeActionMethod(
                    controllerContext,
                    arguments,
                    controller);
                logger.ActionMethodExecuting(controllerContext, orderedArguments);

                var actionResultValueTask = actionMethodExecutor.Execute(objectMethodExecutor, controller, orderedArguments);
                if (actionResultValueTask.IsCompletedSuccessfully)
                {
                    result = actionResultValueTask.Result;
                }
                else
                {
                    result = await actionResultValueTask;
                }

                _result = result;
                logger.ActionMethodExecuted(controllerContext, result);
            }
            finally
            {
                diagnosticSource.AfterActionMethod(
                    controllerContext,
                    arguments,
                    controllerContext,
                    result);
            }
        }

        private static bool IsResultIActionResult(ObjectMethodExecutor executor)
        {
            var resultType = executor.AsyncResultType ?? executor.MethodReturnType;
            return typeof(IActionResult).IsAssignableFrom(resultType);
        }

        private bool IsConvertibleToActionResult(ObjectMethodExecutor executor)
        {
            var resultType = executor.AsyncResultType ?? executor.MethodReturnType;
            return typeof(IConvertToActionResult).IsAssignableFrom(resultType);
        }

        /// <remarks><see cref="ResourceInvoker.InvokeFilterPipelineAsync"/> for details on what the
        /// variables in this method represent.</remarks>
        protected override async Task InvokeInnerFilterAsync()
        {
            var next = State.ActionBegin;
            var scope = Scope.Invoker;
            var state = (object)null;
            var isCompleted = false;

            while (!isCompleted)
            {
                await Next(ref next, ref scope, ref state, ref isCompleted);
            }
        }

        private static void Rethrow(ActionExecutedContext context)
        {
            if (context == null)
            {
                return;
            }

            if (context.ExceptionHandled)
            {
                return;
            }

            if (context.ExceptionDispatchInfo != null)
            {
                context.ExceptionDispatchInfo.Throw();
            }

            if (context.Exception != null)
            {
                throw context.Exception;
            }
        }

        private Task BindArgumentsAsync()
        {
            // Perf: Avoid allocating async state machines where possible. We only need the state
            // machine if you need to bind properties or arguments.
            var actionDescriptor = _controllerContext.ActionDescriptor;
            if (actionDescriptor.BoundProperties.Count == 0 &&
                actionDescriptor.Parameters.Count == 0)
            {
                return Task.CompletedTask;
            }

            Debug.Assert(_cacheEntry.ControllerBinderDelegate != null);
            return _cacheEntry.ControllerBinderDelegate(_controllerContext, _instance, _arguments);
        }

        private static object[] PrepareArguments(
            IDictionary<string, object> actionParameters,
            ObjectMethodExecutor actionMethodExecutor)
        {
            var declaredParameterInfos = actionMethodExecutor.MethodParameters;
            var count = declaredParameterInfos.Length;
            if (count == 0)
            {
                return null;
            }

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

        private enum Scope
        {
            Invoker,
            Action,
        }

        private enum State
        {
            ActionBegin,
            ActionNext,
            ActionAsyncBegin,
            ActionAsyncEnd,
            ActionSyncBegin,
            ActionSyncEnd,
            ActionInside,
            ActionEnd,
        }
    }
}
