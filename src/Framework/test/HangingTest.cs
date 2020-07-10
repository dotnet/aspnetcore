using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTestProject3
{
    public class UnitTest1
    {
        static UnitTest1()
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                if (e.Exception is TestTimeoutException)
                {
                    // Block forever, this allows the test host to collect a dump with the test still on the stack
                    new ManualResetEventSlim().Wait();
                }
            };
        }

        [Fact]
        public async Task AsyncTimeoutTest()
        {
            var tcs = new TaskCompletionSource<object>();

            await tcs.Task.OrTimeout();
        }
    }
    

    static class TaskExtensions
    {
        private const int DefaultTimeout = 5 * 1000;

        public static Task OrTimeout(this ValueTask task, int milliseconds = DefaultTimeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds), memberName, filePath, lineNumber);
        }

        public static Task OrTimeout(this ValueTask task, TimeSpan timeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return task.AsTask().TimeoutAfter(timeout, filePath, lineNumber ?? 0);
        }

        public static Task OrTimeout(this Task task, int milliseconds = DefaultTimeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds), memberName, filePath, lineNumber);
        }

        public static Task OrTimeout(this Task task, TimeSpan timeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return task.TimeoutAfter(timeout, filePath, lineNumber ?? 0);
        }

        public static Task<T> OrTimeout<T>(this ValueTask<T> task, int milliseconds = DefaultTimeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null) =>
            OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds), memberName, filePath, lineNumber);

        public static Task<T> OrTimeout<T>(this ValueTask<T> task, TimeSpan timeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null) =>
            task.AsTask().OrTimeout(timeout, memberName, filePath, lineNumber);

        public static Task<T> OrTimeout<T>(this Task<T> task, int milliseconds = DefaultTimeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds), memberName, filePath, lineNumber);
        }

        public static Task<T> OrTimeout<T>(this Task<T> task, TimeSpan timeout, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int? lineNumber = null)
        {
            return task.TimeoutAfter(timeout, filePath, lineNumber ?? 0);
        }

        public static async Task OrThrowIfOtherFails(this Task task, Task otherTask)
        {
            var completed = await Task.WhenAny(task, otherTask);
            if (completed == otherTask && otherTask.IsFaulted)
            {
                // Manifest the exception
                otherTask.GetAwaiter().GetResult();
                throw new Exception("Unreachable code");
            }
            else
            {
                // Await the task we were asked to await. Either it's finished, or the otherTask finished successfully, and it's not our job to check that
                await task;
            }
        }

        public static async Task<T> OrThrowIfOtherFails<T>(this Task<T> task, Task otherTask)
        {
            await OrThrowIfOtherFails((Task)task, otherTask);

            // If we get here, 'task' is finished and succeeded.
            return task.GetAwaiter().GetResult();
        }

        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = default)
        {
            // Don't create a timer if the task is already completed
            // or the debugger is attached
            if (task.IsCompleted || Debugger.IsAttached)
            {
                return await task;
            }

            var cts = new CancellationTokenSource();
            if (task == await Task.WhenAny(task, Task.Delay(timeout, cts.Token)))
            {
                cts.Cancel();
                return await task;
            }
            else
            {
                throw new TestTimeoutException(CreateMessage(timeout, filePath, lineNumber));
            }
        }

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = default)
        {
            // Don't create a timer if the task is already completed
            // or the debugger is attached
            if (task.IsCompleted || Debugger.IsAttached)
            {
                await task;
                return;
            }

            var cts = new CancellationTokenSource();
            if (task == await Task.WhenAny(task, Task.Delay(timeout, cts.Token)))
            {
                cts.Cancel();
                await task;
            }
            else
            {
                throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
            }
        }

        private static string CreateMessage(TimeSpan timeout, string filePath, int lineNumber)
            => string.IsNullOrEmpty(filePath)
            ? $"The operation timed out after reaching the limit of {timeout.TotalMilliseconds}ms."
            : $"The operation at {filePath}:{lineNumber} timed out after reaching the limit of {timeout.TotalMilliseconds}ms.";
    }

    public class TestTimeoutException : TimeoutException
    {
        public TestTimeoutException(string message) : base(message)
        {

        }
    }    
}
