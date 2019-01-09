using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class HttpConnectionFactoryTests
    {
        [Fact]
        public async Task ConnectionIsDisposedIfItFailsToStartAsync()
        {
            var testHandler = new TestHttpMessageHandler(autoNegotiate: false, handleFirstPoll: false);
            testHandler.OnRequest((req, next, ct) => Task.FromException<HttpResponseMessage>(new Exception("BOOM")));

            var factory = new HttpConnectionFactory(Options.Create(new HttpConnectionOptions() {
                Url = new Uri("http://example.com"),
                HttpMessageHandlerFactory = _ => testHandler
            }), NullLoggerFactory.Instance);

            // We don't care about the specific exception
            await Assert.ThrowsAnyAsync<Exception>(() => factory.ConnectAsync(TransferFormat.Text));

            // We care that the handler (and by extension the client) was disposed
            Assert.True(testHandler.Disposed);
        }
    }
}
