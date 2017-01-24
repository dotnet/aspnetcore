using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public static class TaskExtensions
    {
        private const int DefaultTimeout = 5000;

        public static Task OrTimeout(this Task task, int milliseconds = DefaultTimeout)
        {
            return OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds));
        }

        public static async Task OrTimeout(this Task task, TimeSpan timeout)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeout));
            if (completed != task)
            {
                throw new TimeoutException();
            }

            await task;
        }

        public static Task<T> OrTimeout<T>(this Task<T> task, int milliseconds = DefaultTimeout)
        {
            return OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds));
        }

        public static async Task<T> OrTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeout));
            if (completed != task)
            {
                throw new TimeoutException();
            }

            return await task;
        }
    }
}
