using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class BackgroundHostedServiceTests
    {
        [Fact]
        public void StartReturnsCompletedTaskIfLongRunningTaskIsIncomplete()
        {
            var tcs = new TaskCompletionSource<object>();
            var service = new MyBackgroundService(tcs.Task);

            var task = service.StartAsync(CancellationToken.None);

            Assert.True(task.IsCompleted);
            Assert.False(tcs.Task.IsCompleted);

            // Complete the tsk
            tcs.TrySetResult(null);
        }

        [Fact]
        public void StartReturnsCompletedTaskIfCancelled()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.TrySetCanceled();
            var service = new MyBackgroundService(tcs.Task);

            var task = service.StartAsync(CancellationToken.None);

            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task StartReturnsLongRunningTaskIfFailed()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.TrySetException(new Exception("fail!"));
            var service = new MyBackgroundService(tcs.Task);

            var exception = await Assert.ThrowsAsync<Exception>(() => service.StartAsync(CancellationToken.None));

            Assert.Equal("fail!", exception.Message);
        }

        [Fact]
        public async Task StopAsyncWithoutStartAsyncNoops()
        {
            var tcs = new TaskCompletionSource<object>();
            var service = new MyBackgroundService(tcs.Task);

            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task StopAsyncStopsBackgroundService()
        {
            var tcs = new TaskCompletionSource<object>();
            var service = new MyBackgroundService(tcs.Task);

            await service.StartAsync(CancellationToken.None);

            Assert.False(service.ExecuteTask.IsCompleted);

            await service.StopAsync(CancellationToken.None);

            Assert.True(service.ExecuteTask.IsCompleted);
        }

        [Fact]
        public async Task StopAsyncStopsEvenIfTaskNeverEnds()
        {
            var service = new IgnoreCancellationService();

            await service.StartAsync(CancellationToken.None);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await service.StopAsync(cts.Token);
        }

        [Fact]
        public async Task StopAsyncThrowsIfCancellationCallbackThrows()
        {
            var service = new ThrowOnCancellationService();

            await service.StartAsync(CancellationToken.None);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await Assert.ThrowsAsync<AggregateException>(() => service.StopAsync(cts.Token));

            Assert.Equal(2, service.TokenCalls);
        }

        [Fact]
        public async Task StartAsyncThenDisposeTriggersCancelledToken()
        {
            var service = new WaitForCancelledTokenService();

            await service.StartAsync(CancellationToken.None);

            service.Dispose();
        }

        private class WaitForCancelledTokenService : BackgroundService
        {
            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                return Task.Delay(Timeout.Infinite, stoppingToken);
            }
        }

        private class ThrowOnCancellationService : BackgroundService
        {
            public int TokenCalls { get; set; }

            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                stoppingToken.Register(() =>
                {
                    TokenCalls++;
                    throw new InvalidOperationException();
                });

                stoppingToken.Register(() =>
                {
                    TokenCalls++;
                });

                return new TaskCompletionSource<object>().Task;
            }
        }

        private class IgnoreCancellationService : BackgroundService
        {
            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                return new TaskCompletionSource<object>().Task;
            }
        }

        private class MyBackgroundService : BackgroundService
        {
            private readonly Task _task;

            public Task ExecuteTask { get; set; }

            public MyBackgroundService(Task task)
            {
                _task = task;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                ExecuteTask = ExecuteCore(stoppingToken);
                await ExecuteTask;
            }

            private async Task ExecuteCore(CancellationToken stoppingToken)
            {
                var task = await Task.WhenAny(_task, Task.Delay(Timeout.Infinite, stoppingToken));

                await task;
            }
        }
    }
}
