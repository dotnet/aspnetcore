// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Internal;
using Resources = Microsoft.AspNetCore.Mvc.Core.Resources;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal abstract class ActionMethodExecutor
{
    private static readonly ActionMethodExecutor[] Executors = new ActionMethodExecutor[]
    {
            // Executors for sync methods
            new VoidResultExecutor(),
            new SyncActionResultExecutor(),
            new SyncObjectResultExecutor(),

            // Executors for async methods
            new TaskResultExecutor(),
            new AwaitableResultExecutor(),
            new TaskOfIActionResultExecutor(),
            new TaskOfActionResultExecutor(),
            new AwaitableObjectResultExecutor(),
    };

    public static EmptyResult EmptyResultInstance { get; } = new();

    public abstract ValueTask<IActionResult> Execute(
        ActionContext actionContext,
        IActionResultTypeMapper mapper,
        ObjectMethodExecutor executor,
        object controller,
        object?[]? arguments);

    protected abstract bool CanExecute(ObjectMethodExecutor executor);

    public abstract ValueTask<object?> Execute(ControllerEndpointFilterInvocationContext invocationContext);

    public static ActionMethodExecutor GetExecutor(ObjectMethodExecutor executor)
    {
        for (var i = 0; i < Executors.Length; i++)
        {
            if (Executors[i].CanExecute(executor))
            {
                return Executors[i];
            }
        }

        throw new UnreachableException();
    }

    public static ActionMethodExecutor GetFilterExecutor(ControllerActionDescriptor actionDescriptor) =>
        new FilterActionMethodExecutor(actionDescriptor);

    private sealed class FilterActionMethodExecutor : ActionMethodExecutor
    {
        private readonly ControllerActionDescriptor _controllerActionDescriptor;

        public FilterActionMethodExecutor(ControllerActionDescriptor controllerActionDescriptor)
        {
            _controllerActionDescriptor = controllerActionDescriptor;
        }

        public override async ValueTask<IActionResult> Execute(
            ActionContext actionContext,
            IActionResultTypeMapper mapper,
            ObjectMethodExecutor executor,
            object controller,
            object?[]? arguments)
        {
            var context = new ControllerEndpointFilterInvocationContext(_controllerActionDescriptor, actionContext, executor, mapper, controller, arguments);
            var result = await _controllerActionDescriptor.FilterDelegate!(context);
            return ConvertToActionResult(mapper, result, executor.IsMethodAsync ? executor.AsyncResultType! : executor.MethodReturnType);
        }

        public override ValueTask<object?> Execute(ControllerEndpointFilterInvocationContext invocationContext)
        {
            // This is never called
            throw new NotSupportedException();
        }

        protected override bool CanExecute(ObjectMethodExecutor executor)
        {
            // This is never called
            throw new NotSupportedException();
        }
    }

    // void LogMessage(..)
    private sealed class VoidResultExecutor : ActionMethodExecutor
    {
        public override ValueTask<IActionResult> Execute(
            ActionContext actionContext,
            IActionResultTypeMapper mapper,
            ObjectMethodExecutor executor,
            object controller,
            object?[]? arguments)
        {
            executor.Execute(controller, arguments);
            return new(EmptyResultInstance);
        }

        public override ValueTask<object?> Execute(ControllerEndpointFilterInvocationContext invocationContext)
        {
            var executor = invocationContext.Executor;
            var controller = invocationContext.Controller;
            var arguments = (object[])invocationContext.Arguments;

            executor.Execute(controller, arguments);
            return new(EmptyResultInstance);
        }

        protected override bool CanExecute(ObjectMethodExecutor executor)
            => !executor.IsMethodAsync && executor.MethodReturnType == typeof(void);
    }

    // IActionResult Post(..)
    // CreatedAtResult Put(..)
    private sealed class SyncActionResultExecutor : ActionMethodExecutor
    {
        public override ValueTask<IActionResult> Execute(
            ActionContext actionContext,
            IActionResultTypeMapper mapper,
            ObjectMethodExecutor executor,
            object controller,
            object?[]? arguments)
        {
            var actionResult = (IActionResult)executor.Execute(controller, arguments)!;
            EnsureActionResultNotNull(executor, actionResult);

            return new(actionResult);
        }

        public override ValueTask<object?> Execute(ControllerEndpointFilterInvocationContext invocationContext)
        {
            var executor = invocationContext.Executor;
            var controller = invocationContext.Controller;
            var arguments = (object[])invocationContext.Arguments;

            var actionResult = (IActionResult)executor.Execute(controller, arguments)!;
            EnsureActionResultNotNull(executor, actionResult);

            return new(actionResult);
        }

        protected override bool CanExecute(ObjectMethodExecutor executor)
            => !executor.IsMethodAsync && typeof(IActionResult).IsAssignableFrom(executor.MethodReturnType);
    }

    // Person GetPerson(..)
    // object Index(..)
    private sealed class SyncObjectResultExecutor : ActionMethodExecutor
    {
        public override ValueTask<IActionResult> Execute(
            ActionContext actionContext,
            IActionResultTypeMapper mapper,
            ObjectMethodExecutor executor,
            object controller,
            object?[]? arguments)
        {
            // Sync method returning arbitrary object
            var returnValue = executor.Execute(controller, arguments);
            var actionResult = ConvertToActionResult(mapper, returnValue, executor.MethodReturnType);
            return new(actionResult);
        }

        public override ValueTask<object?> Execute(ControllerEndpointFilterInvocationContext invocationContext)
        {
            var executor = invocationContext.Executor;
            var controller = invocationContext.Controller;
            var arguments = (object[])invocationContext.Arguments;
            var mapper = invocationContext.Mapper;

            // Sync method returning arbitrary object
            var returnValue = executor.Execute(controller, arguments);
            var actionResult = ConvertToActionResult(mapper, returnValue, executor.MethodReturnType);
            return new(actionResult);
        }

        // Catch-all for sync methods
        protected override bool CanExecute(ObjectMethodExecutor executor) => !executor.IsMethodAsync;
    }

    // Task SaveState(..)
    private sealed class TaskResultExecutor : ActionMethodExecutor
    {
        public override async ValueTask<IActionResult> Execute(
            ActionContext actionContext,
            IActionResultTypeMapper mapper,
            ObjectMethodExecutor executor,
            object controller,
            object?[]? arguments)
        {
            await (Task)executor.Execute(controller, arguments)!;
            return EmptyResultInstance;
        }

        public override async ValueTask<object?> Execute(ControllerEndpointFilterInvocationContext invocationContext)
        {
            var executor = invocationContext.Executor;
            var controller = invocationContext.Controller;
            var arguments = (object[])invocationContext.Arguments;

            await (Task)executor.Execute(controller, arguments)!;
            return EmptyResultInstance;
        }

        protected override bool CanExecute(ObjectMethodExecutor executor) => executor.MethodReturnType == typeof(Task);
    }

    // CustomAsync PerformActionAsync(..)
    // Custom task-like type with no return value.
    private sealed class AwaitableResultExecutor : ActionMethodExecutor
    {
        public override async ValueTask<IActionResult> Execute(
            ActionContext actionContext,
            IActionResultTypeMapper mapper,
            ObjectMethodExecutor executor,
            object controller,
            object?[]? arguments)
        {
            await executor.ExecuteAsync(controller, arguments);
            return EmptyResultInstance;
        }

        public override async ValueTask<object?> Execute(ControllerEndpointFilterInvocationContext invocationContext)
        {
            var executor = invocationContext.Executor;
            var controller = invocationContext.Controller;
            var arguments = (object[])invocationContext.Arguments;

            await executor.ExecuteAsync(controller, arguments);
            return EmptyResultInstance;
        }

        protected override bool CanExecute(ObjectMethodExecutor executor)
        {
            // Async method returning void
            return executor.IsMethodAsync && executor.AsyncResultType == typeof(void);
        }
    }

    // Task<IActionResult> Post(..)
    private sealed class TaskOfIActionResultExecutor : ActionMethodExecutor
    {
        public override async ValueTask<IActionResult> Execute(
            ActionContext actionContext,
            IActionResultTypeMapper mapper,
            ObjectMethodExecutor executor,
            object controller,
            object?[]? arguments)
        {
            // Async method returning Task<IActionResult>
            // Avoid extra allocations by calling Execute rather than ExecuteAsync and casting to Task<IActionResult>.
            var returnValue = executor.Execute(controller, arguments);
            var actionResult = await (Task<IActionResult>)returnValue!;
            EnsureActionResultNotNull(executor, actionResult);

            return actionResult;
        }

        public override async ValueTask<object?> Execute(ControllerEndpointFilterInvocationContext invocationContext)
        {
            var executor = invocationContext.Executor;
            var controller = invocationContext.Controller;
            var arguments = (object[])invocationContext.Arguments;

            // Async method returning Task<IActionResult>
            // Avoid extra allocations by calling Execute rather than ExecuteAsync and casting to Task<IActionResult>.
            var returnValue = executor.Execute(controller, arguments);
            var actionResult = await (Task<IActionResult>)returnValue!;
            EnsureActionResultNotNull(executor, actionResult);

            return actionResult;
        }

        protected override bool CanExecute(ObjectMethodExecutor executor)
            => typeof(Task<IActionResult>).IsAssignableFrom(executor.MethodReturnType);
    }

    // Task<PhysicalFileResult> DownloadFile(..)
    // ValueTask<ViewResult> GetViewsAsync(..)
    private sealed class TaskOfActionResultExecutor : ActionMethodExecutor
    {
        public override async ValueTask<IActionResult> Execute(
            ActionContext actionContext,
            IActionResultTypeMapper mapper,
            ObjectMethodExecutor executor,
            object controller,
            object?[]? arguments)
        {
            // Async method returning awaitable-of-IActionResult (e.g., Task<ViewResult>)
            // We have to use ExecuteAsync because we don't know the awaitable's type at compile time.
            var actionResult = (IActionResult)await executor.ExecuteAsync(controller, arguments);
            EnsureActionResultNotNull(executor, actionResult);
            return actionResult;
        }

        public override async ValueTask<object?> Execute(ControllerEndpointFilterInvocationContext invocationContext)
        {
            var executor = invocationContext.Executor;
            var controller = invocationContext.Controller;
            var arguments = (object[])invocationContext.Arguments;

            // Async method returning awaitable-of-IActionResult (e.g., Task<ViewResult>)
            // We have to use ExecuteAsync because we don't know the awaitable's type at compile time.
            var actionResult = (IActionResult)await executor.ExecuteAsync(controller, arguments);
            EnsureActionResultNotNull(executor, actionResult);
            return actionResult;
        }

        protected override bool CanExecute(ObjectMethodExecutor executor)
        {
            // Async method returning awaitable-of - IActionResult(e.g., Task<ViewResult>)
            return executor.IsMethodAsync && typeof(IActionResult).IsAssignableFrom(executor.AsyncResultType);
        }
    }

    // Task<object> GetPerson(..)
    // Task<Customer> GetCustomerAsync(..)
    private sealed class AwaitableObjectResultExecutor : ActionMethodExecutor
    {
        public override async ValueTask<IActionResult> Execute(
            ActionContext actionContext,
            IActionResultTypeMapper mapper,
            ObjectMethodExecutor executor,
            object controller,
            object?[]? arguments)
        {
            // Async method returning awaitable-of-nonvoid
            var returnValue = await executor.ExecuteAsync(controller, arguments);
            var actionResult = ConvertToActionResult(mapper, returnValue, executor.AsyncResultType!);
            return actionResult;
        }

        public override async ValueTask<object?> Execute(ControllerEndpointFilterInvocationContext invocationContext)
        {
            var executor = invocationContext.Executor;
            var controller = invocationContext.Controller;
            var arguments = (object[])invocationContext.Arguments;
            var mapper = invocationContext.Mapper;

            var returnValue = await executor.ExecuteAsync(controller, arguments);
            var actionResult = ConvertToActionResult(mapper, returnValue, executor.AsyncResultType!);
            return actionResult;
        }

        protected override bool CanExecute(ObjectMethodExecutor executor) => true;
    }

    private static void EnsureActionResultNotNull(ObjectMethodExecutor executor, IActionResult actionResult)
    {
        if (actionResult == null)
        {
            var type = executor.AsyncResultType ?? executor.MethodReturnType;
            throw new InvalidOperationException(Resources.FormatActionResult_ActionReturnValueCannotBeNull(type));
        }
    }

    private static IActionResult ConvertToActionResult(IActionResultTypeMapper mapper, object? returnValue, Type declaredType)
    {
        var result = (returnValue as IActionResult) ?? mapper.Convert(returnValue, declaredType);
        if (result == null)
        {
            throw new InvalidOperationException(Resources.FormatActionResult_ActionReturnValueCannotBeNull(declaredType));
        }

        return result;
    }
}
