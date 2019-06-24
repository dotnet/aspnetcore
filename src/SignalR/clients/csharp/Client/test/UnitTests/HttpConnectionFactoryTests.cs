using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
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

            var factory = new HttpConnectionFactory(
                Mock.Of<IHubProtocol>(p => p.TransferFormat == TransferFormat.Text),
                Options.Create(new HttpConnectionOptions() {
                    HttpMessageHandlerFactory = _ => testHandler
                }),
                NullLoggerFactory.Instance);

            // We don't care about the specific exception
            await Assert.ThrowsAnyAsync<Exception>(async () => await factory.ConnectAsync(new HttpEndPoint(new Uri("http://example.com"))));

            // We care that the handler (and by extension the client) was disposed
            Assert.True(testHandler.Disposed);
        }
    }
}
