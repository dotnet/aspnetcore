using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Schema;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class HubProxyGeneratorTests
    {
        public interface IMyHub
        {
            Task GetNothing();
            Task<int> GetScalar();
            Task<List<int>> GetCollection();
            Task<int> SetScalar(int a);
            Task<List<int>> SetCollection(List<int> a);
            Task<ChannelReader<int>> StreamToClientViaChannel();
            Task<ChannelReader<int>> StreamToClientViaChannelWithToken(CancellationToken cancellationToken);
            IAsyncEnumerable<int> StreamToClientViaEnumerableWithToken(CancellationToken cancellationToken);
            Task StreamFromClientViaChannel(ChannelReader<int> reader);
            Task StreamFromClientViaEnumerable(IAsyncEnumerable<int> reader);
            Task<ChannelReader<int>> StreamBidirectionalViaChannel(ChannelReader<float> reader);
            Task<ChannelReader<int>> StreamBidirectionalViaChannelWithToken(ChannelReader<float> reader, CancellationToken cancellationToken);
            IAsyncEnumerable<int> StreamBidirectionalViaEnumerable(IAsyncEnumerable<float> reader);
            IAsyncEnumerable<int> StreamBidirectionalViaEnumerableWithToken(IAsyncEnumerable<float> reader, CancellationToken cancellationToken);
            ValueTask ReturnValueTask();
            ValueTask<int> ReturnGenericValueTask();
        }

        [Fact]
        public async Task GetNothing()
        {
            // Arrange
            var mockConn = new Mock<IHubConnection>(MockBehavior.Strict);
            mockConn
                .Setup(x => x.InvokeCoreAsync(
                    nameof(IMyHub.GetNothing),
                    typeof(object),
                    Array.Empty<object>(),
                    default))
                .Returns(Task.FromResult(default(object)));
            var conn = mockConn.Object;
            var myHub = conn.GetProxy<IMyHub>();

            // Act
            await myHub.GetNothing();

            // Assert
            mockConn.VerifyAll();
        }

        [Fact]
        public async Task GetScalar()
        {
            // Arrange
            var mockConn = new Mock<IHubConnection>(MockBehavior.Strict);
            mockConn
                .Setup(x => x.InvokeCoreAsync(
                    nameof(IMyHub.GetScalar),
                    typeof(int),
                    Array.Empty<object>(),
                    default))
                .Returns(Task.FromResult((object) 10));
            var conn = mockConn.Object;
            var myHub = conn.GetProxy<IMyHub>();

            // Act
            var result = await myHub.GetScalar();

            // Assert
            mockConn.VerifyAll();
            Assert.Equal(10, result);
        }

        [Fact]
        public async Task GetCollection()
        {
            // Arrange
            var mockConn = new Mock<IHubConnection>(MockBehavior.Strict);
            mockConn
                .Setup(x => x.InvokeCoreAsync(
                    nameof(IMyHub.GetCollection),
                    typeof(List<int>),
                    Array.Empty<object>(),
                    default))
                .Returns(Task.FromResult((object) new List<int>{ 10 }));
            var conn = mockConn.Object;
            var myHub = conn.GetProxy<IMyHub>();

            // Act
            var result = await myHub.GetCollection();

            // Assert
            mockConn.VerifyAll();
            Assert.NotNull(result);
            Assert.Collection(result, item => Assert.Equal(10, item));
        }

        [Fact]
        public async Task SetScalar()
        {
            // Arrange
            var mockConn = new Mock<IHubConnection>(MockBehavior.Strict);
            mockConn
                .Setup(x => x.InvokeCoreAsync(
                    nameof(IMyHub.SetScalar),
                    typeof(int),
                    It.Is<object[]>(y => ((object[])y).Any(z => (int)z == 20)),
                    default))
                .Returns(Task.FromResult((object) 10));
            var conn = mockConn.Object;
            var myHub = conn.GetProxy<IMyHub>();

            // Act
            var result = await myHub.SetScalar(20);

            // Assert
            mockConn.VerifyAll();
            Assert.Equal(10, result);
        }

        [Fact]
        public async Task SetCollection()
        {
            // Arrange
            var arg = new List<int>() {20};
            var mockConn = new Mock<IHubConnection>(MockBehavior.Strict);
            mockConn
                .Setup(x => x.InvokeCoreAsync(
                    nameof(IMyHub.SetCollection),
                    typeof(List<int>),
                    It.Is<object[]>(y => ((object[])y).Any(z => (List<int>)z == arg)),
                    default))
                .Returns(Task.FromResult((object) new List<int>{ 10 }));
            var conn = mockConn.Object;
            var myHub = conn.GetProxy<IMyHub>();

            // Act
            var result = await myHub.SetCollection(arg);

            // Assert
            mockConn.VerifyAll();
            Assert.NotNull(result);
            Assert.Collection(result, item => Assert.Equal(10, item));
        }
    }
}
