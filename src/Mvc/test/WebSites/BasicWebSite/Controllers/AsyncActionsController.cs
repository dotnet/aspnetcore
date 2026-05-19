// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Controllers;

public class AsyncActionsController : Controller
{
    const int SimulateDelayMilliseconds = 20;

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        // So that tests can observe we're following the proper flow after an exception, surface a
        // message saying what the exception was.
        if (context.Exception != null)
        {
            context.Result = Content($"Action exception message: {context.Exception.Message}");
            context.ExceptionHandled = true;
        }
    }

    public async Task<IActionResult> ActionWithSuffixAsync()
    {
        await Task.Yield();
        return Ok();
    }

    public Task<IActionResult> ActionReturningViewAsync()
    {
        return Task.FromResult<IActionResult>(View());
    }

    public async void AsyncVoidAction()
    {
        await Task.Delay(SimulateDelayMilliseconds);
    }

    public Task TaskAction()
    {
        return Task.Delay(SimulateDelayMilliseconds);
    }

    public async Task TaskExceptionAction()
    {
        await Task.Delay(SimulateDelayMilliseconds);
        throw new CustomException();
    }

    public async Task<Message> TaskOfObjectAction(string message)
    {
        await Task.Delay(SimulateDelayMilliseconds);
        return new Message { Text = message };
    }

    public async Task<Message> TaskOfObjectExceptionAction(string message)
    {
        await Task.Delay(SimulateDelayMilliseconds);
        throw new CustomException();
    }

    public async Task<IActionResult> TaskOfIActionResultAction(string message)
    {
        await Task.Delay(SimulateDelayMilliseconds);
        return Content(message);
    }

    public async Task<IActionResult> TaskOfIActionResultExceptionAction(string message)
    {
        await Task.Delay(SimulateDelayMilliseconds);
        throw new CustomException();
    }

    public async Task<ContentResult> TaskOfContentResultAction(string message)
    {
        await Task.Delay(SimulateDelayMilliseconds);
        return Content(message);
    }

    public async Task<ContentResult> TaskOfContentResultExceptionAction(string message)
    {
        await Task.Delay(SimulateDelayMilliseconds);
        throw new CustomException();
    }

    public ValueTask<Message> PreCompletedValueTaskOfObjectAction(string message)
    {
        return new ValueTask<Message>(new Message { Text = message });
    }

    public ValueTask<Message> PreCompletedValueTaskOfObjectExceptionAction(string message)
    {
        throw new CustomException();
    }

    public ValueTask<IActionResult> PreCompletedValueTaskOfIActionResultAction(string message)
    {
        return new ValueTask<IActionResult>(Content(message));
    }

    public ValueTask<IActionResult> PreCompletedValueTaskOfIActionResultExceptionAction(string message)
    {
        throw new CustomException();
    }

    public ValueTask<ContentResult> PreCompletedValueTaskOfContentResultAction(string message)
    {
        return new ValueTask<ContentResult>(Content(message));
    }

    public ValueTask<ContentResult> PreCompletedValueTaskOfContentResultExceptionAction(string message)
    {
        throw new CustomException();
    }

    public CustomAwaitable CustomAwaitableVoidAction()
    {
        return new CustomAwaitable(SimulateDelayMilliseconds);
    }

    public CustomAwaitable CustomAwaitableVoidExceptionAction()
    {
        throw new CustomException();
    }

    public CustomAwaitable<Message> CustomAwaitableOfObjectAction(string message)
    {
        return new CustomAwaitable<Message>(
            SimulateDelayMilliseconds,
            new Message { Text = message });
    }

    public CustomAwaitable<Message> CustomAwaitableOfObjectExceptionAction(string message)
    {
        throw new CustomException();
    }

    public CustomAwaitable<IActionResult> CustomAwaitableOfIActionResultAction(string message)
    {
        return new CustomAwaitable<IActionResult>(SimulateDelayMilliseconds, Content(message));
    }

    public CustomAwaitable<IActionResult> CustomAwaitableOfIActionResultExceptionAction(string message)
    {
        throw new CustomException();
    }

    public CustomAwaitable<ContentResult> CustomAwaitableOfContentResultAction(string message)
    {
        return new CustomAwaitable<ContentResult>(SimulateDelayMilliseconds, Content(message));
    }

    public CustomAwaitable<ContentResult> CustomAwaitableOfContentResultExceptionAction(string message)
    {
        throw new CustomException();
    }

    public class Message
    {
        public string Text { get; set; }
    }

    public class CustomAwaitable
    {
        protected readonly int _simulateDelayMilliseconds;

        public CustomAwaitable(int simulateDelayMilliseconds)
        {
            _simulateDelayMilliseconds = simulateDelayMilliseconds;
        }

        public CustomAwaiter GetAwaiter()
        {
            return new CustomAwaiter(_simulateDelayMilliseconds);
        }
    }

    public class CustomAwaitable<T> : CustomAwaitable
    {
        private readonly T _result;

        public CustomAwaitable(int simulateDelayMilliseconds, T result)
            : base(simulateDelayMilliseconds)
        {
            _result = result;
        }

        public new CustomAwaiter<T> GetAwaiter()
        {
            return new CustomAwaiter<T>(_simulateDelayMilliseconds, _result);
        }
    }

    public class CustomAwaiter : INotifyCompletion
    {
        private readonly IList<Action> _continuations = new List<Action>();

        public CustomAwaiter(int simulateDelayMilliseconds)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(simulateDelayMilliseconds);
                lock (_continuations)
                {
                    IsCompleted = true;

                    foreach (var continuation in _continuations)
                    {
                        continuation();
                    }

                    _continuations.Clear();
                }
            });
        }

        public bool IsCompleted { get; private set; }

        public void OnCompleted(Action continuation)
        {
            lock (_continuations)
            {
                if (IsCompleted)
                {
                    continuation();
                }
                else
                {
                    _continuations.Add(continuation);
                }
            }
        }

        public void GetResult()
        {
        }
    }

    public class CustomAwaiter<T> : CustomAwaiter
    {
        private readonly T _result;

        public CustomAwaiter(int simulateDelayMilliseconds, T result)
            : base(simulateDelayMilliseconds)
        {
            _result = result;
        }

        public new T GetResult() => _result;
    }

    public class CustomException : Exception
    {
        public CustomException() : base("This is a custom exception.")
        {
        }
    }
}
