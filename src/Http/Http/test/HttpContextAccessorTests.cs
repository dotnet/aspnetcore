// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
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

            Assert.Same(context, accessor.HttpContext);
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
            context.TraceIdentifier = "1";
            accessor.HttpContext = context;

            var checkAsyncFlowTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var waitForNullTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var afterNullCheckTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                // The HttpContext flows with the execution context
                Assert.Same(context, accessor.HttpContext);

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
            context.TraceIdentifier = null;

            waitForNullTcs.SetResult(null);

            Assert.Null(accessor.HttpContext);

            await afterNullCheckTcs.Task;
        }

        [Fact]
        public async Task HttpContextAccessor_GettingHttpContextReturnsNullHttpContextIfDifferentTraceIdentifier()
        {
            var accessor = new HttpContextAccessor();

            var context = new DefaultHttpContext();
            context.TraceIdentifier = "1";
            accessor.HttpContext = context;

            var checkAsyncFlowTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var waitForNullTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var afterNullCheckTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                // The HttpContext flows with the execution context
                Assert.Same(context, accessor.HttpContext);

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

            // Reset the trace identifier on the first request
            context.TraceIdentifier = null;

            // Set a new http context
            var context2 = new DefaultHttpContext();
            context2.TraceIdentifier = "2";
            accessor.HttpContext = context2;

            waitForNullTcs.SetResult(null);

            Assert.Same(context2, accessor.HttpContext);

            await afterNullCheckTcs.Task;
        }

        [Fact]
        public async Task HttpContextAccessor_GettingHttpContextDoesNotFlowIfAccessorSetToNull()
        {
            var accessor = new HttpContextAccessor();

            var context = new DefaultHttpContext();
            context.TraceIdentifier = "1";
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
            context.TraceIdentifier = "1";
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