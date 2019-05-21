using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Aspnetcore.RequestThrottling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    public class MiddlewareTests
    {
        [Fact]
        public async Task RequestsCanEnterIfSpaceAvailible()
        {
            var middleware = TestUtils.CreateTestMiddleWare(maxConcurrentRequests: 1);
            var context = new DefaultHttpContext();

            // a request should go through with no problems
            await middleware.Invoke(context).OrTimeout();
        }

        [Fact]
        public async Task RequestsAreBlockedIfNoSpaceAvailible()
        {
            var blocker = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstRequest = true;

            var middleware = TestUtils.CreateTestMiddleWare(
                maxConcurrentRequests: 1,
                next: httpContext =>
                {
                    if (firstRequest)
                    {
                        firstRequest = false;
                        return blocker.Task;
                    }
                    return Task.CompletedTask;
                });

            // t1 (as the first request) is blocked by the tcs blocker
            var t1 = middleware.Invoke(new DefaultHttpContext());

            // t2 is blocked from entering the server since t1 already exists there
            // note: increasing MaxConcurrentRequests would allow t2 through while t1 is blocked
            var t2 = middleware.Invoke(new DefaultHttpContext());

            Assert.False(t1.IsCompleted);
            Assert.False(t2.IsCompleted);

            blocker.SetResult("t1 completes");
            await t1.OrTimeout();
            await t2.OrTimeout();
        }
    }
}
