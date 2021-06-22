using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class CallbackRegistrationGeneratorTests
    {
        public interface IMyClient
        {
            void NoArg();
            void SingleArg(int a);
            void ManyArgs(int a, float b);
        }

        private class MyClient : IMyClient
        {
            public int CallsOfNoArg;
            public void NoArg()
            {
                CallsOfNoArg += 1;
            }

            public List<int> CallsOfSingleArg = new();
            public void SingleArg(int a)
            {
                CallsOfSingleArg.Add(a);
            }

            public List<(int, float)> CallsOfManyArgs = new();
            public void ManyArgs(int a, float b)
            {
                CallsOfManyArgs.Add((a, b));
            }
        }

        private class Disposable : IDisposable
        {
            public bool IsDisposed;
            public void Dispose() => IsDisposed = true;
        }

        [Fact]
        public void RegistersCallbackProvider()
        {
            // Arrange
            var mockConn = new Mock<IHubConnection>(MockBehavior.Strict);
            var noArgReg = new Disposable();
            mockConn
                .Setup(x => x.On(
                    "NoArg",
                    Array.Empty<Type>(),
                    It.IsAny<Func<object[], object, Task>>(),
                    It.IsAny<object>()))
                .Returns(noArgReg);
            var singleArgReg = new Disposable();
            mockConn
                .Setup(x => x.On(
                    "SingleArg",
                    It.Is<Type[]>(t => t.Length == 1 && t[0] == typeof(int)),
                    It.IsAny<Func<object[], object, Task>>(),
                    It.IsAny<object>()))
                .Returns(singleArgReg);
            var manyArgsReg = new Disposable();
            mockConn
                .Setup(x => x.On(
                    "ManyArgs",
                    It.Is<Type[]>(t => t.Length == 2 && t[0] == typeof(int) && t[1] == typeof(float)),
                    It.IsAny<Func<object[], object, Task>>(),
                    It.IsAny<object>()))
                .Returns(manyArgsReg);
            var conn = mockConn.Object;
            var myClient = new MyClient();

            // Act
            var registration = conn.RegisterCallbackProvider<IMyClient>(myClient);

            // Assert
            mockConn.VerifyAll();
            Assert.False(noArgReg.IsDisposed);
            Assert.False(singleArgReg.IsDisposed);
            Assert.False(manyArgsReg.IsDisposed);
        }

        [Fact]
        public void UnregistersCallbackProvider()
        {
            // Arrange
            var mockConn = new Mock<IHubConnection>(MockBehavior.Strict);
            var noArgReg = new Disposable();
            mockConn
                .Setup(x => x.On(
                    "NoArg",
                    Array.Empty<Type>(),
                    It.IsAny<Func<object[], object, Task>>(),
                    It.IsAny<object>()))
                .Returns(noArgReg);
            var singleArgReg = new Disposable();
            mockConn
                .Setup(x => x.On(
                    "SingleArg",
                    It.Is<Type[]>(t => t.Length == 1 && t[0] == typeof(int)),
                    It.IsAny<Func<object[], object, Task>>(),
                    It.IsAny<object>()))
                .Returns(singleArgReg);
            var manyArgsReg = new Disposable();
            mockConn
                .Setup(x => x.On(
                    "ManyArgs",
                    It.Is<Type[]>(t => t.Length == 2 && t[0] == typeof(int) && t[1] == typeof(float)),
                    It.IsAny<Func<object[], object, Task>>(),
                    It.IsAny<object>()))
                .Returns(manyArgsReg);
            var conn = mockConn.Object;
            var myClient = new MyClient();
            var registration = conn.RegisterCallbackProvider<IMyClient>(myClient);

            // Act
            registration.Dispose();

            // Assert
            Assert.True(noArgReg.IsDisposed);
            Assert.True(singleArgReg.IsDisposed);
            Assert.True(manyArgsReg.IsDisposed);
        }

        [Fact]
        public async Task CallbacksGetTriggered()
        {
            // Arrange
            var mockConn = new Mock<IHubConnection>(MockBehavior.Strict);
            var noArgReg = new Disposable();
            Func<object[], object, Task> noArgFunc = null;
            object noArgState = null;
            mockConn
                .Setup(x => x.On(
                    "NoArg",
                    Array.Empty<Type>(),
                    It.IsAny<Func<object[], object, Task>>(),
                    It.IsAny<object>()))
                .Callback(
                    (string methodName, Type[] parameterTypes, Func<object[], object, Task> handler, object state) =>
                    {
                        noArgFunc = handler;
                        noArgState = state;
                    })
                .Returns(noArgReg);
            Func<object[], object, Task> singleArgFunc = null;
            object singleArgState = null;
            var singleArgReg = new Disposable();
            mockConn
                .Setup(x => x.On(
                    "SingleArg",
                    It.Is<Type[]>(t => t.Length == 1 && t[0] == typeof(int)),
                    It.IsAny<Func<object[], object, Task>>(),
                    It.IsAny<object>()))
                .Callback(
                    (string methodName, Type[] parameterTypes, Func<object[], object, Task> handler, object state) =>
                    {
                        singleArgFunc = handler;
                        singleArgState = state;
                    })
                .Returns(singleArgReg);
            Func<object[], object, Task> manyArgsFunc = null;
            object manyArgsState = null;
            var manyArgsReg = new Disposable();
            mockConn
                .Setup(x => x.On(
                    "ManyArgs",
                    It.Is<Type[]>(t => t.Length == 2 && t[0] == typeof(int) && t[1] == typeof(float)),
                    It.IsAny<Func<object[], object, Task>>(),
                    It.IsAny<object>()))
                .Callback(
                    (string methodName, Type[] parameterTypes, Func<object[], object, Task> handler, object state) =>
                    {
                        manyArgsFunc = handler;
                        manyArgsState = state;
                    })
                .Returns(manyArgsReg);
            var conn = mockConn.Object;
            var myClient = new MyClient();
            var registration = conn.RegisterCallbackProvider<IMyClient>(myClient);

            // Act + Assert
            Assert.NotNull(noArgFunc);
            await noArgFunc(Array.Empty<object>(), noArgState);
            Assert.Equal(1, myClient.CallsOfNoArg);

            Assert.NotNull(singleArgFunc);
            await singleArgFunc(new object[]{10}, singleArgState);
            Assert.Single(myClient.CallsOfSingleArg);
            Assert.Equal(10, myClient.CallsOfSingleArg[0]);

            Assert.NotNull(manyArgsFunc);
            await singleArgFunc(new object[]{10, 5.5f}, manyArgsState);
            Assert.Single(myClient.CallsOfManyArgs);
            Assert.Equal((10, 5.5f), myClient.CallsOfManyArgs[0]);
        }
    }
}
