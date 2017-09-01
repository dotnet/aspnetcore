// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    internal abstract class ActionMethodExecutor
    {
        private static readonly ActionMethodExecutor[] Executors = new ActionMethodExecutor[]
        {
            // Executors for sync methods
            new VoidResultExecutor(),
            new SyncActionResultExecutor(),
            new SyncObjectResultExecutor(),

            // Executors for async methods
            new AwaitableResultExecutor(),
            new TaskResultExecutor(),
            new TaskOfIActionResultExecutor(),
            new TaskOfActionResultExecutor(),
            new AwaitableObjectResultExecutor(),
        };

        public abstract ValueTask<IActionResult> Execute(ObjectMethodExecutor executor, object controller, object[] arguments);

        protected abstract bool CanExecute(ObjectMethodExecutor executor);

        public static ActionMethodExecutor GetExecutor(ObjectMethodExecutor executor)
        {
            for (var i = 0; i < Executors.Length; i++)
            {
                if (Executors[i].CanExecute(executor))
                {
                    return Executors[i];
                }
            }

            Debug.Fail("Should not get here");
            throw new Exception();
        }

        // void LogMessage(..)
        private class VoidResultExecutor : ActionMethodExecutor
        {
            public override ValueTask<IActionResult> Execute(ObjectMethodExecutor executor, object controller, object[] arguments)
            {
                executor.Execute(controller, arguments);
                return new ValueTask<IActionResult>(new EmptyResult());
            }

            protected override bool CanExecute(ObjectMethodExecutor executor)
                => !executor.IsMethodAsync && executor.MethodReturnType == typeof(void);
        }

        // IActionResult Post(..)
        // CreatedAtResult Put(..)
        private class SyncActionResultExecutor : ActionMethodExecutor
        {
            public override ValueTask<IActionResult> Execute(ObjectMethodExecutor executor, object controller, object[] arguments)
            {
                var actionResult = (IActionResult)executor.Execute(controller, arguments);
                EnsureActionResultNotNull(executor, actionResult);

                return new ValueTask<IActionResult>(actionResult);
            }

            protected override bool CanExecute(ObjectMethodExecutor executor)
                => !executor.IsMethodAsync && typeof(IActionResult).IsAssignableFrom(executor.MethodReturnType);
        }

        // Person GetPerson(..)
        // object Index(..)
        private class SyncObjectResultExecutor : ActionMethodExecutor
        {
            public override ValueTask<IActionResult> Execute(ObjectMethodExecutor executor, object controller, object[] arguments)
            {
                // Sync method returning arbitrary object
                var returnValue = executor.Execute(controller, arguments);
                var actionResult = ConvertToActionResult(returnValue, executor.MethodReturnType);
                return new ValueTask<IActionResult>(actionResult);
            }

            // Catch-all for sync methods
            protected override bool CanExecute(ObjectMethodExecutor executor) => !executor.IsMethodAsync;
        }

        // Task SaveState(..)
        private class TaskResultExecutor : ActionMethodExecutor
        {
            public override async ValueTask<IActionResult> Execute(ObjectMethodExecutor executor, object controller, object[] arguments)
            {
                await (Task)executor.Execute(controller, arguments);
                return new EmptyResult();
            }

            protected override bool CanExecute(ObjectMethodExecutor executor) => executor.MethodReturnType == typeof(Task);
        }

        // CustomAsync PerformActionAsync(..)
        // Custom task-like type with no return value.
        private class AwaitableResultExecutor : ActionMethodExecutor
        {
            public override async ValueTask<IActionResult> Execute(ObjectMethodExecutor executor, object controller, object[] arguments)
            {
                await executor.ExecuteAsync(controller, arguments);
                return new EmptyResult();
            }

            protected override bool CanExecute(ObjectMethodExecutor executor)
            {
                // Async method returning void
                return executor.IsMethodAsync && executor.AsyncResultType == typeof(void);
            }
        }

        // Task<IActionResult> Post(..)
        private class TaskOfIActionResultExecutor : ActionMethodExecutor
        {
            public override async ValueTask<IActionResult> Execute(ObjectMethodExecutor executor, object controller, object[] arguments)
            {
                // Async method returning Task<IActionResult>
                // Avoid extra allocations by calling Execute rather than ExecuteAsync and casting to Task<IActionResult>.
                var returnValue = executor.Execute(controller, arguments);
                var actionResult = await (Task<IActionResult>)returnValue;
                EnsureActionResultNotNull(executor, actionResult);

                return actionResult;
            }

            protected override bool CanExecute(ObjectMethodExecutor executor)
                => typeof(Task<IActionResult>).IsAssignableFrom(executor.MethodReturnType);
        }

        // Task<PhysicalfileResult> DownloadFile(..)
        // ValueTask<ViewResult> GetViewsAsync(..)
        private class TaskOfActionResultExecutor : ActionMethodExecutor
        {
            public override async ValueTask<IActionResult> Execute(ObjectMethodExecutor executor, object controller, object[] arguments)
            {
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
        private class AwaitableObjectResultExecutor : ActionMethodExecutor
        {
            public override async ValueTask<IActionResult> Execute(ObjectMethodExecutor executor, object controller, object[] arguments)
            {
                // Async method returning awaitable-of-nonvoid
                var returnValue = await executor.ExecuteAsync(controller, arguments);
                var actionResult = ConvertToActionResult(returnValue, executor.MethodReturnType);
                return actionResult;
            }

            protected override bool CanExecute(ObjectMethodExecutor executor) => true;
        }

        private static void EnsureActionResultNotNull(ObjectMethodExecutor executor, IActionResult actionResult)
        {
            if (actionResult == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatActionResult_ActionReturnValueCannotBeNull(executor.AsyncResultType ?? executor.MethodReturnType));
            }
        }

        private static IActionResult ConvertToActionResult(object returnValue, Type declaredType)
        {
            IActionResult result;
            switch (returnValue)
            {
                case IActionResult actionResult:
                    result = actionResult;
                    break;
                case IConvertToActionResult convertToActionResult:
                    result = convertToActionResult.Convert();
                    break;
                default:
                    result = new ObjectResult(returnValue)
                    {
                        DeclaredType = declaredType,
                    };
                    break;
            }

            if (result == null)
            {
                throw new InvalidOperationException(Resources.FormatActionResult_ActionReturnValueCannotBeNull(declaredType));
            }

            return result;
        }
    }
}
