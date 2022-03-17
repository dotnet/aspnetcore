// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

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
        accessor.HttpContext = context;

        var checkAsyncFlowTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waitForNullTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var afterNullCheckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ThreadPool.QueueUserWorkItem(async _ =>
        {
            // The HttpContext flows with the execution context
            Assert.Same(context, accessor.HttpContext);

            checkAsyncFlowTcs.SetResult();

            await waitForNullTcs.Task;

            try
            {
                Assert.Null(accessor.HttpContext);

                afterNullCheckTcs.SetResult();
            }
            catch (Exception ex)
            {
                afterNullCheckTcs.SetException(ex);
            }
        });

        await checkAsyncFlowTcs.Task;

        // Null out the accessor
        accessor.HttpContext = null;

        waitForNullTcs.SetResult();

        Assert.Null(accessor.HttpContext);

        await afterNullCheckTcs.Task;
    }

    [Fact]
    public async Task HttpContextAccessor_GettingHttpContextReturnsNullHttpContextIfChanged()
    {
        var accessor = new HttpContextAccessor();

        var context = new DefaultHttpContext();
        accessor.HttpContext = context;

        var checkAsyncFlowTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waitForNullTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var afterNullCheckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ThreadPool.QueueUserWorkItem(async _ =>
        {
            // The HttpContext flows with the execution context
            Assert.Same(context, accessor.HttpContext);

            checkAsyncFlowTcs.SetResult();

            await waitForNullTcs.Task;

            try
            {
                Assert.Null(accessor.HttpContext);

                afterNullCheckTcs.SetResult();
            }
            catch (Exception ex)
            {
                afterNullCheckTcs.SetException(ex);
            }
        });

        await checkAsyncFlowTcs.Task;

        // Set a new http context
        var context2 = new DefaultHttpContext();
        accessor.HttpContext = context2;

        waitForNullTcs.SetResult();

        Assert.Same(context2, accessor.HttpContext);

        await afterNullCheckTcs.Task;
    }

    [Fact]
    public async Task HttpContextAccessor_GettingHttpContextDoesNotFlowIfAccessorSetToNull()
    {
        var accessor = new HttpContextAccessor();

        var context = new DefaultHttpContext();
        accessor.HttpContext = context;

        var checkAsyncFlowTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        accessor.HttpContext = null;

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                // The HttpContext flows with the execution context
                Assert.Null(accessor.HttpContext);
                checkAsyncFlowTcs.SetResult();
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

        var checkAsyncFlowTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            try
            {
                // The HttpContext flows with the execution context
                Assert.Null(accessor.HttpContext);
                checkAsyncFlowTcs.SetResult();
            }
            catch (Exception ex)
            {
                checkAsyncFlowTcs.SetException(ex);
            }
        }, null);

        await checkAsyncFlowTcs.Task;
    }
}
