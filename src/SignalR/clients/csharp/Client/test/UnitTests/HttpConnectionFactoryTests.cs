using System;
using System.Net;
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

        [Fact]
        public async Task DoesNotSupportNonHttpEndPoints()
        {
            var factory = new HttpConnectionFactory(
                Mock.Of<IHubProtocol>(p => p.TransferFormat == TransferFormat.Text),
                Options.Create(new HttpConnectionOptions()),
                NullLoggerFactory.Instance);

            var ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await factory.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 0)));

            Assert.Equal("The provided EndPoint must be of type HttpEndPoint.", ex.Message);
        }

        [Fact]
        public async Task OptionsUrlMustMatchEndPointIfSet()
        {
            var url1 = new Uri("http://example.com/1");
            var url2 = new Uri("http://example.com/2");

            var factory = new HttpConnectionFactory(
                Mock.Of<IHubProtocol>(p => p.TransferFormat == TransferFormat.Text),
                Options.Create(new HttpConnectionOptions
                {
                    Url = url1
                }),
                NullLoggerFactory.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await factory.ConnectAsync(new HttpEndPoint(url2)));
            Assert.Equal("If HttpConnectionOptions.Url was set, it must match the HttpEndPoint.Url passed to ConnectAsync.", ex.Message);
        }
    }
}
