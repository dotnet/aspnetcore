using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests
{
    public class DelegateTests
    {
        [Fact]
        public void AttachToQueueForDelegation()
        {
            var queueName = Guid.NewGuid().ToString();
            using var server = Utilities.CreateHttpServer(out var baseAddress, httpContext =>
            {
                return Task.FromResult(0);
            },
            options =>
            {
                options.RequestQueueName = queueName;
            }) as MessagePump;

            // Assert.DoesNotThrow
            var requestQueue = new RequestQueue(null, queueName, RequestQueueMode.Delegate, NullLogger.Instance);

            // Assert.DoesNotThrow
            var urlGroup = new UrlGroup(requestQueue, UrlPrefix.Create(baseAddress));
            requestQueue.UrlGroup = urlGroup;
        }

        private async Task<string> SendRequestAsync(string uri)
        {
            using var client = new HttpClient();
            return await client.GetStringAsync(uri);
        }
    }
}
