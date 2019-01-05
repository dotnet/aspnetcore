// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http
{
    public class HttpContextAccessorTests
    {
        [Fact]
        public async Task HttpContextAccessor_GettingHttpContextReturnsHttpContext()
        {
            var accessor = new HttpContextAccessor();

            var context = new DefaultHttpContext();
            context.TraceIdentifier = "1";
            accessor.HttpContext = context;

            await Task.Delay(100);

            Assert.True(accessor.HttpContext.Equals(context));
        }

        [Fact]
        public void HttpContextAccessor_GettingHttpContextWithOutSettingReturnsNull()
        {
            var accessor = new HttpContextAccessor();

            Assert.Null(accessor.HttpContext);
        }

        [Fact]
        public async Task HttpContextAccessor_GettingHttpContextReturnsNullHttpContextIfSetToNull()
        {
            var accessor = new HttpContextAccessor();

            var context = new DefaultHttpContext();
            accessor.HttpContext = context;

            var checkAsyncFlowTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var waitForNullTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var afterNullCheckTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                // The HttpContext flows with the execution context
                Assert.True(accessor.HttpContext.Equals(context));

                checkAsyncFlowTcs.SetResult(null);

                await waitForNullTcs.Task;

                try
                {
                    Assert.Null(accessor.HttpContext);

                    afterNullCheckTcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    afterNullCheckTcs.SetException(ex);
                }
            });

            await checkAsyncFlowTcs.Task;

            // Null out the accessor
            accessor.HttpContext = null;

            waitForNullTcs.SetResult(null);

            Assert.Null(accessor.HttpContext);

            await afterNullCheckTcs.Task;
        }

        [Fact]
        public async Task HttpContextAccessor_GettingHttpContextReturnsNullHttpContextIfChanged()
        {
            var accessor = new HttpContextAccessor();
            accessor.HttpContext = new DefaultHttpContext();

            var context = accessor.HttpContext;
            Assert.NotNull(context);

            var checkAsyncFlowTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var waitForNullTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var afterNullCheckTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                // The HttpContext flows with the execution context
                Assert.True(accessor.HttpContext.Equals(context));

                checkAsyncFlowTcs.SetResult(null);

                await waitForNullTcs.Task;

                try
                {
                    Assert.Null(accessor.HttpContext);

                    afterNullCheckTcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    afterNullCheckTcs.SetException(ex);
                }
            });

            await checkAsyncFlowTcs.Task;

            // Set a new http context
            accessor.HttpContext = new DefaultHttpContext();
            var context2 = accessor.HttpContext;

            waitForNullTcs.SetResult(null);

            Assert.Same(context2, accessor.HttpContext);
            Assert.NotSame(context, context2);

            await afterNullCheckTcs.Task;
        }


        [Fact]
        public async Task HttpContextAccessor_CallingMethodsOnHttpContextThrowsObjectDisposedIfChanged()
        {
            var accessor = new HttpContextAccessor();
            accessor.HttpContext = new DefaultHttpContext();
            var context = accessor.HttpContext;

            var checkAsyncFlowTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var waitForNullTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var afterNullCheckTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                // The HttpContext flows with the execution context
                Assert.True(accessor.HttpContext.Equals(context));

                checkAsyncFlowTcs.SetResult(null);

                await waitForNullTcs.Task;

                try
                {
                    Assert.Null(accessor.HttpContext);
                    Assert.NotNull(context);

                    Assert.Throws<ObjectDisposedException>(() => context.Features);
                    Assert.Throws<ObjectDisposedException>(() => context.Request);
                    Assert.Throws<ObjectDisposedException>(() => context.Response);
                    Assert.Throws<ObjectDisposedException>(() => context.Connection);
                    Assert.Throws<ObjectDisposedException>(() => context.WebSockets);
                    Assert.Throws<ObjectDisposedException>(() => context.User);
                    Assert.Throws<ObjectDisposedException>(() => context.User = null);
                    Assert.Throws<ObjectDisposedException>(() => context.Items);
                    Assert.Throws<ObjectDisposedException>(() => context.Items = null);
                    Assert.Throws<ObjectDisposedException>(() => context.RequestServices);
                    Assert.Throws<ObjectDisposedException>(() => context.RequestServices = null);
                    Assert.Throws<ObjectDisposedException>(() => context.RequestAborted);
                    Assert.Throws<ObjectDisposedException>(() => context.RequestAborted = default);
                    Assert.Throws<ObjectDisposedException>(() => context.TraceIdentifier);
                    Assert.Throws<ObjectDisposedException>(() => context.TraceIdentifier = null);
                    Assert.Throws<ObjectDisposedException>(() => context.Session);
                    Assert.Throws<ObjectDisposedException>(() => context.Session = null);
                    Assert.Throws<ObjectDisposedException>(() => context.Abort());

                    afterNullCheckTcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    afterNullCheckTcs.SetException(ex);
                }
            });

            await checkAsyncFlowTcs.Task;

            // Set a new http context
            accessor.HttpContext = new DefaultHttpContext();
            var context2 = accessor.HttpContext;

            waitForNullTcs.SetResult(null);

            Assert.Same(context2, accessor.HttpContext);
            Assert.NotSame(context, context2);

            await afterNullCheckTcs.Task;
        }

        [Fact]
        public async Task HttpContextAccessor_GettingHttpContextDoesNotFlowIfAccessorSetToNull()
        {
            var accessor = new HttpContextAccessor();

            var context = new DefaultHttpContext();
            accessor.HttpContext = context;

            var checkAsyncFlowTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            accessor.HttpContext = null;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // The HttpContext flows with the execution context
                    Assert.Null(accessor.HttpContext);
                    checkAsyncFlowTcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    checkAsyncFlowTcs.SetException(ex);
                }
            });

            await checkAsyncFlowTcs.Task;
        }

        [Fact]
        public async Task HttpContextAccessor_GettingHttpContextDoesNotFlowIfExecutionContextDoesNotFlow()
        {
            var accessor = new HttpContextAccessor();

            var context = new DefaultHttpContext();
            accessor.HttpContext = context;

            var checkAsyncFlowTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                try
                {
                    // The HttpContext flows with the execution context
                    Assert.Null(accessor.HttpContext);
                    checkAsyncFlowTcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    checkAsyncFlowTcs.SetException(ex);
                }
            }, null);

            await checkAsyncFlowTcs.Task;
        }
    }
}
