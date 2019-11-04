// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.Rendering
{
    public class RendererSynchronizationContextTest
    {
        // Nothing should exceed the timeout in a successful run of the the tests, this is just here to catch
        // failures.
        public TimeSpan Timeout = Debugger.IsAttached ? System.Threading.Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10);

        [Fact]
        public void Post_RunsAsynchronously_WhenNotBusy()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            var thread = Thread.CurrentThread;
            Thread capturedThread = null;

            var e = new ManualResetEventSlim();
            
            // Act
            context.Post((_) =>
            {
                capturedThread = Thread.CurrentThread;

                e.Set();
            }, null);

            // Assert
            Assert.True(e.Wait(Timeout), "timeout");
            Assert.NotSame(thread, capturedThread);
        }

        [Fact]
        public void Post_RunsAsynchronously_WhenNotBusy_Exception()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            Exception exception = null;
            context.UnhandledException += (sender, e) =>
            {
                exception = (InvalidTimeZoneException)e.ExceptionObject;
            };

            // Act
            context.Post((_) =>
            {
                throw new InvalidTimeZoneException();
            }, null);

            // Assert
            //
            // Use another item to 'push through' the throwing one
            context.Send((_) => { }, null);
            Assert.NotNull(exception);
        }

        [Fact]
        public async Task Post_CanRunAsynchronously_WhenBusy()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            var thread = Thread.CurrentThread;
            Thread capturedThread = null;

            var e1 = new ManualResetEventSlim();
            var e2 = new ManualResetEventSlim();
            var e3 = new ManualResetEventSlim();

            var task = Task.Run(() =>
            {
                context.Send((_) =>
                {
                    e1.Set();
                    Assert.True(e2.Wait(Timeout), "timeout");
                }, null);
            });

            Assert.True(e1.Wait(Timeout), "timeout");

            // Act
            context.Post((_) =>
            {
                capturedThread = Thread.CurrentThread;

                e3.Set();
            }, null);

            // Assert
            Assert.False(e2.IsSet);
            e2.Set(); // Unblock the first item
            await task;

            Assert.True(e3.Wait(Timeout), "timeout");
            Assert.NotSame(thread, capturedThread);
        }

        [Fact]
        public async Task Post_CanRunAsynchronously_CaptureExecutionContext()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            // CultureInfo uses the execution context.
            CultureInfo.CurrentCulture = new CultureInfo("en-GB");
            CultureInfo capturedCulture = null;

            SynchronizationContext capturedContext = null;

            var e1 = new ManualResetEventSlim();
            var e2 = new ManualResetEventSlim();
            var e3 = new ManualResetEventSlim();

            var task = Task.Run(() =>
            {
                context.Send((_) =>
                {
                    e1.Set();
                    Assert.True(e2.Wait(Timeout), "timeout");
                }, null);
            });

            Assert.True(e1.Wait(Timeout), "timeout");

            // Act
            SynchronizationContext original = SynchronizationContext.Current;

            try
            {
                SynchronizationContext.SetSynchronizationContext(context);
                context.Post((_) =>
                {
                    capturedCulture = CultureInfo.CurrentCulture;
                    capturedContext = SynchronizationContext.Current;
                    e3.Set();
                }, null);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }

            // Assert
            Assert.False(e2.IsSet);
            e2.Set(); // Unblock the first item
            await task;

            Assert.True(e3.Wait(Timeout), "timeout");
            Assert.Same(CultureInfo.CurrentCulture, capturedCulture);
            Assert.Same(context, capturedContext);
        }

        [Fact]
        public async Task Post_CanRunAsynchronously_WhenBusy_Exception()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            Exception exception = null;
            context.UnhandledException += (sender, e) =>
            {
                exception = (InvalidTimeZoneException)e.ExceptionObject;
            };

            var e1 = new ManualResetEventSlim();
            var e2 = new ManualResetEventSlim();

            var task = Task.Run(() =>
            {
                context.Send((_) =>
                {
                    e1.Set();
                    Assert.True(e2.Wait(Timeout), "timeout");
                }, null);
            });

            Assert.True(e1.Wait(Timeout), "timeout");

            // Act
            context.Post((_) =>
            {
                throw new InvalidTimeZoneException();
            }, null);

            // Assert
            Assert.False(e2.IsSet);
            e2.Set(); // Unblock the first item
            await task;

            // Use another item to 'push through' the throwing one
            context.Send((_) => { }, null);
            Assert.NotNull(exception);
        }

        [Fact]
        public async Task Post_BackgroundWorkItem_CanProcessMoreItemsInline()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            Thread capturedThread = null;

            var e1 = new ManualResetEventSlim();
            var e2 = new ManualResetEventSlim();
            var e3 = new ManualResetEventSlim();
            var e4 = new ManualResetEventSlim();
            var e5 = new ManualResetEventSlim();
            var e6 = new ManualResetEventSlim();

            // Force task2 to execute in the background
            var task1 = Task.Run(() => context.Send((_) =>
            {
                e1.Set();
                Assert.True(e2.Wait(Timeout), "timeout");
            }, null));

            Assert.True(e1.Wait(Timeout), "timeout");

            var task2 = Task.Run(() =>
            {
                context.Send((_) =>
                {
                    e3.Set();
                    Assert.True(e4.Wait(Timeout), "timeout");
                    capturedThread = Thread.CurrentThread;
                }, null);
            });

            e2.Set();
            await task1;

            Assert.True(e3.Wait(Timeout), "timeout");

            // Act
            //
            // Now task2 is 'running' in the sync context. Schedule more work items - they will be
            // run immediately after the second item
            context.Post((_) =>
            {
                e5.Set();
                Assert.Same(Thread.CurrentThread, capturedThread);
            }, null);
            context.Post((_) =>
            {
                e6.Set();
                Assert.Same(Thread.CurrentThread, capturedThread);
            }, null);


            // Assert
            e4.Set();
            await task2;

            Assert.True(e5.Wait(Timeout), "timeout");
            Assert.True(e6.Wait(Timeout), "timeout");
        }

        [Fact]
        public void Post_CapturesContext()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            var e1 = new ManualResetEventSlim();

            // CultureInfo uses the execution context.
            CultureInfo.CurrentCulture = new CultureInfo("en-GB");
            CultureInfo capturedCulture = null;

            SynchronizationContext capturedContext = null;

            // Act
            context.Post(async (_) =>
            {
                await Task.Yield();

                capturedCulture = CultureInfo.CurrentCulture;
                capturedContext = SynchronizationContext.Current;
                e1.Set();
            }, null);

            // Assert
            Assert.True(e1.Wait(Timeout), "timeout");
            Assert.Same(CultureInfo.CurrentCulture, capturedCulture);
            Assert.Same(context, capturedContext);
        }

        [Fact]
        public void Send_CanRunSynchronously()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            var thread = Thread.CurrentThread;
            Thread capturedThread = null;

            // Act
            context.Send((_) =>
            {
                capturedThread = Thread.CurrentThread;
            }, null);

            // Assert
            Assert.Same(thread, capturedThread);
        }

        [Fact]
        public void Send_CanRunSynchronously_Exception()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            // Act & Assert
            Assert.Throws<InvalidTimeZoneException>(() => context.Send((_) =>
            {
                throw new InvalidTimeZoneException();
            }, null));
        }

        [Fact]
        public async Task Send_BlocksWhenOtherWorkRunning()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            var e1 = new ManualResetEventSlim();
            var e2 = new ManualResetEventSlim();
            var e3 = new ManualResetEventSlim();
            var e4 = new ManualResetEventSlim();

            // Force task2 to execute in the background
            var task1 = Task.Run(() =>
            {
                context.Send((_) =>
                {
                    e1.Set();
                    Assert.True(e2.Wait(Timeout), "timeout");
                }, null);
            });

            Assert.True(e1.Wait(Timeout), "timeout");

            // Act
            //
            // Dispatch this on the background thread because otherwise it would block.
            var task2 = Task.Run(() =>
            {
                e3.Set();
                context.Send((_) =>
                {
                    e4.Set();
                }, null);
            });

            // Assert
            Assert.True(e3.Wait(Timeout), "timeout");
            Assert.True(e3.IsSet);

            // Unblock the first item
            e2.Set();
            await task1;

            await task2;
            Assert.True(e4.IsSet);
        }

        [Fact]
        public void Send_CapturesContext()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            var e1 = new ManualResetEventSlim();

            // CultureInfo uses the execution context.
            CultureInfo.CurrentCulture = new CultureInfo("en-GB");
            CultureInfo capturedCulture = null;

            SynchronizationContext capturedContext = null;

            // Act
            context.Send(async (_) =>
            {
                await Task.Yield();

                capturedCulture = CultureInfo.CurrentCulture;
                capturedContext = SynchronizationContext.Current;

                e1.Set();
            }, null);

            // Assert
            Assert.True(e1.Wait(Timeout), "timeout");
            Assert.Same(CultureInfo.CurrentCulture, capturedCulture);
            Assert.Same(context, capturedContext);
        }

        [Fact]
        public async Task InvokeAsync_Action_CanRunSynchronously_WhenNotBusy()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            var thread = Thread.CurrentThread;
            Thread capturedThread = null;

            // Act
            var task = context.InvokeAsync(() =>
            {
                capturedThread = Thread.CurrentThread;
            });

            // Assert
            await task;
            Assert.Same(thread, capturedThread);
        }

        [Fact]
        public async Task InvokeAsync_Action_CanRunAsynchronously_WhenBusy()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            var thread = Thread.CurrentThread;
            Thread capturedThread = null;

            var e1 = new ManualResetEventSlim();
            var e2 = new ManualResetEventSlim();
            var e3 = new ManualResetEventSlim();

            var task1 = Task.Run(() =>
            {
                context.Send((_) =>
                {
                    e1.Set();
                    Assert.True(e2.Wait(Timeout), "timeout");
                }, null);
            });

            Assert.True(e1.Wait(Timeout), "timeout");

            var task2 = context.InvokeAsync(() =>
            {
                capturedThread = Thread.CurrentThread;

                e3.Set();
            });

            // Assert
            Assert.False(e2.IsSet);
            e2.Set(); // Unblock the first item
            await task1;

            Assert.True(e3.Wait(Timeout), "timeout");
            await task2;
            Assert.NotSame(thread, capturedThread);
        }

        [Fact]
        public async Task InvokeAsync_Action_CanRethrowExceptions()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            // Act
            var task = context.InvokeAsync((Action)(() =>
            {
                throw new InvalidTimeZoneException();
            }));

            // Assert
            await Assert.ThrowsAsync<InvalidTimeZoneException>(async () => await task);
        }

        [Fact]
        public async Task InvokeAsync_Action_CanReportCancellation()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            // Act
            var task = context.InvokeAsync((Action)(() =>
            {
                throw new OperationCanceledException();
            }));

            // Assert
            Assert.Equal(TaskStatus.Canceled, task.Status);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }

        [Fact]
        public async Task InvokeAsync_FuncT_CanRunSynchronously_WhenNotBusy()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            var thread = Thread.CurrentThread;

            // Act
            var task = context.InvokeAsync(() =>
            {
                return Thread.CurrentThread;
            });

            // Assert
            Assert.Same(thread, await task);
        }

        [Fact]
        public async Task InvokeAsync_FuncT_CanRunAsynchronously_WhenBusy()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            var thread = Thread.CurrentThread;

            var e1 = new ManualResetEventSlim();
            var e2 = new ManualResetEventSlim();
            var e3 = new ManualResetEventSlim();

            var task1 = Task.Run(() =>
            {
                context.Send((_) =>
                {
                    e1.Set();
                    Assert.True(e2.Wait(Timeout), "timeout");
                }, null);
            });

            Assert.True(e1.Wait(Timeout), "timeout");

            var task2 = context.InvokeAsync(() =>
            {
                e3.Set();

                return Thread.CurrentThread;
            });

            // Assert
            Assert.False(e2.IsSet);
            e2.Set(); // Unblock the first item
            await task1;

            Assert.True(e3.Wait(Timeout), "timeout");
            Assert.NotSame(thread, await task2);
        }

        [Fact]
        public async Task InvokeAsync_FuncT_CanRethrowExceptions()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            // Act
            var task = context.InvokeAsync<string>((Func<string>)(() =>
            {
                throw new InvalidTimeZoneException();
            }));

            // Assert
            await Assert.ThrowsAsync<InvalidTimeZoneException>(async () => await task);
        }

        [Fact]
        public async Task InvokeAsync_FuncT_CanReportCancellation()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            // Act
            var task = context.InvokeAsync<string>((Func<string>)(() =>
            {
                throw new OperationCanceledException();
            }));

            // Assert
            Assert.Equal(TaskStatus.Canceled, task.Status);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }

        [Fact]
        public async Task InvokeAsync_FuncTask_CanRunSynchronously_WhenNotBusy()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            var thread = Thread.CurrentThread;
            Thread capturedThread = null;

            // Act
            var task = context.InvokeAsync(() =>
            {
                capturedThread = Thread.CurrentThread;
                return Task.CompletedTask;
            });

            // Assert
            await task;
            Assert.Same(thread, capturedThread);
        }

        [Fact]
        public async Task InvokeAsync_FuncTask_CanRunAsynchronously_WhenBusy()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            var thread = Thread.CurrentThread;
            Thread capturedThread = null;

            var e1 = new ManualResetEventSlim();
            var e2 = new ManualResetEventSlim();
            var e3 = new ManualResetEventSlim();

            var task1 = Task.Run(() =>
            {
                context.Send((_) =>
                {
                    e1.Set();
                    Assert.True(e2.Wait(Timeout), "timeout");
                }, null);
            });

            Assert.True(e1.Wait(Timeout), "timeout");

            var task2 = context.InvokeAsync(() =>
            {
                capturedThread = Thread.CurrentThread;

                e3.Set();
                return Task.CompletedTask;
            });

            // Assert
            Assert.False(e2.IsSet);
            e2.Set(); // Unblock the first item
            await task1;

            Assert.True(e3.Wait(Timeout), "timeout");
            await task2;
            Assert.NotSame(thread, capturedThread);
        }

        [Fact]
        public async Task InvokeAsync_FuncTask_CanRethrowExceptions()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            // Act
            var task = context.InvokeAsync(() =>
            {
                throw new InvalidTimeZoneException();
            });

            // Assert
            await Assert.ThrowsAsync<InvalidTimeZoneException>(async () => await task);
        }

        [Fact]
        public async Task InvokeAsync_FuncTask_CanReportCancellation()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            // Act
            var task = context.InvokeAsync(() =>
            {
                throw new OperationCanceledException();
            });

            // Assert
            Assert.Equal(TaskStatus.Canceled, task.Status);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }

        [Fact]
        public async Task InvokeAsync_FuncTaskT_CanRunSynchronously_WhenNotBusy()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            var thread = Thread.CurrentThread;

            // Act
            var task = context.InvokeAsync(() =>
            {
                return Task.FromResult(Thread.CurrentThread);
            });

            // Assert
            Assert.Same(thread, await task);
        }

        [Fact]
        public async Task InvokeAsync_FuncTaskT_CanRunAsynchronously_WhenBusy()
        {
            // Arrange
            var context = new RendererSynchronizationContext();
            var thread = Thread.CurrentThread;

            var e1 = new ManualResetEventSlim();
            var e2 = new ManualResetEventSlim();
            var e3 = new ManualResetEventSlim();

            var task1 = Task.Run(() =>
            {
                context.Send((_) =>
                {
                    e1.Set();
                    Assert.True(e2.Wait(Timeout), "timeout");
                }, null);
            });

            Assert.True(e1.Wait(Timeout), "timeout");

            var task2 = context.InvokeAsync(() =>
            {
                e3.Set();

                return Task.FromResult(Thread.CurrentThread);
            });

            // Assert
            Assert.False(e2.IsSet);
            e2.Set(); // Unblock the first item
            await task1;

            Assert.True(e3.Wait(Timeout), "timeout");
            Assert.NotSame(thread, await task2);
        }

        [Fact]
        public async Task InvokeAsync_FuncTaskT_CanRethrowExceptions()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            // Act
            var task = context.InvokeAsync<string>((Func<Task<string>>)(() =>
            {
                throw new InvalidTimeZoneException();
            }));

            // Assert
            await Assert.ThrowsAsync<InvalidTimeZoneException>(async () => await task);
        }

        [Fact]
        public async Task InvokeAsync_FuncTaskT_CanReportCancellation()
        {
            // Arrange
            var context = new RendererSynchronizationContext();

            // Act
            var task = context.InvokeAsync<string>((Func<Task<string>>)(() =>
            {
                throw new OperationCanceledException();
            }));

            // Assert
            Assert.Equal(TaskStatus.Canceled, task.Status);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }
    }
}
