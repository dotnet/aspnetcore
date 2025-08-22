// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.SignalR.Tests.Internal;

public class TypedClientBuilderTests
{
    [Fact]
    public async Task ProducesImplementationThatProxiesMethodsToIClientProxyAsync()
    {
        var clientProxy = new MockProxy();
        var typedProxy = TypedClientBuilder<ITestClient>.Build(clientProxy);

        var objArg = new object();
        var task = typedProxy.Method("foo", 42, objArg);
        Assert.False(task.IsCompleted);

        Assert.Collection(clientProxy.Sends,
            send =>
            {
                Assert.Equal("Method", send.Method);
                Assert.Equal("foo", send.Arguments[0]);
                Assert.Equal(42, send.Arguments[1]);
                Assert.Equal(CancellationToken.None, send.CancellationToken);
                Assert.Same(objArg, send.Arguments[2]);
                send.Complete();
            });

        await task.DefaultTimeout();
    }

    [Fact]
    public async Task ProducesImplementationThatProxiesMethodsToIRenamedClientProxyAsync()
    {
        var clientProxy = new MockProxy();
        var typedProxy = TypedClientBuilder<IRenamedTestClient>.Build(clientProxy);

        var objArg = new object();
        var task = typedProxy.MethodAsync("foo", 42, objArg);
        Assert.False(task.IsCompleted);

        Assert.Collection(clientProxy.Sends,
            send =>
            {
                Assert.Equal("Method", send.Method);
                Assert.Equal("foo", send.Arguments[0]);
                Assert.Equal(42, send.Arguments[1]);
                Assert.Equal(CancellationToken.None, send.CancellationToken);
                Assert.Same(objArg, send.Arguments[2]);
                send.Complete();
            });

        await task.DefaultTimeout();
    }

    [Fact]
    public async Task SupportsSubInterfaces()
    {
        var clientProxy = new MockProxy();
        var typedProxy = TypedClientBuilder<IInheritedClient>.Build(clientProxy);

        var objArg = new object();
        var task1 = typedProxy.Method("foo", 42, objArg);
        Assert.False(task1.IsCompleted);

        var task2 = typedProxy.SubMethod("bar");
        Assert.False(task2.IsCompleted);

        Assert.Collection(clientProxy.Sends,
            send1 =>
            {
                Assert.Equal("Method", send1.Method);
                Assert.Collection(send1.Arguments,
                    arg1 => Assert.Equal("foo", arg1),
                    arg2 => Assert.Equal(42, arg2),
                    arg3 => Assert.Same(objArg, arg3));
                Assert.Equal(CancellationToken.None, send1.CancellationToken);
                send1.Complete();
            },
            send2 =>
            {
                Assert.Equal("SubMethod", send2.Method);
                Assert.Collection(send2.Arguments,
                    arg1 => Assert.Equal("bar", arg1));
                Assert.Equal(CancellationToken.None, send2.CancellationToken);
                send2.Complete();
            });

        await task1.DefaultTimeout();
        await task2.DefaultTimeout();
    }

    [Fact]
    public async Task SupportsCancellationToken()
    {
        var clientProxy = new MockProxy();
        var typedProxy = TypedClientBuilder<ICancellationTokenMethod>.Build(clientProxy);
        CancellationTokenSource cts1 = new CancellationTokenSource();
        var task1 = typedProxy.Method("foo", cts1.Token);
        Assert.False(task1.IsCompleted);

        CancellationTokenSource cts2 = new CancellationTokenSource();
        var task2 = typedProxy.NoArgumentMethod(cts2.Token);
        Assert.False(task2.IsCompleted);

        Assert.Collection(clientProxy.Sends,
            send1 =>
            {
                Assert.Equal("Method", send1.Method);
                Assert.Single(send1.Arguments);
                Assert.Collection(send1.Arguments,
                    arg1 => Assert.Equal("foo", arg1));
                Assert.Equal(cts1.Token, send1.CancellationToken);
                send1.Complete();
            },
            send2 =>
            {
                Assert.Equal("NoArgumentMethod", send2.Method);
                Assert.Empty(send2.Arguments);
                Assert.Equal(cts2.Token, send2.CancellationToken);
                send2.Complete();
            });

        await task1.DefaultTimeout();
        await task2.DefaultTimeout();
    }

    [Fact]
    public void ThrowsIfProvidedAClass()
    {
        var clientProxy = new MockProxy();
        var ex = Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<object>.Build(clientProxy));
        Assert.Equal("Type must be an interface.", ex.Message);
    }

    [Fact]
    public void ThrowsIfProvidedAStruct()
    {
        var clientProxy = new MockProxy();
        var ex = Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<ValueTask>.Build(clientProxy));
        Assert.Equal("Type must be an interface.", ex.Message);
    }

    [Fact]
    public void ThrowsIfProvidedADelegate()
    {
        var clientProxy = new MockProxy();
        var ex = Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<EventHandler>.Build(clientProxy));
        Assert.Equal("Type must be an interface.", ex.Message);
    }

    [Fact]
    public void ThrowsIfInterfaceHasVoidReturningMethod()
    {
        var clientProxy = new MockProxy();
        var ex = Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IVoidMethodClient>.Build(clientProxy));
        Assert.Equal($"Cannot generate proxy implementation for '{typeof(IVoidMethodClient).FullName}.{nameof(IVoidMethodClient.Method)}'. All client proxy methods must return '{typeof(Task).FullName}' or '{typeof(Task).FullName}<T>'.", ex.Message);
    }

    [Fact]
    public void ThrowsIfInterfaceHasNonTaskReturns()
    {
        var clientProxy = new MockProxy();
        var ex = Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IStringMethodClient>.Build(clientProxy));
        Assert.Equal($"Cannot generate proxy implementation for '{typeof(IStringMethodClient).FullName}.{nameof(IStringMethodClient.Method)}'. All client proxy methods must return '{typeof(Task).FullName}' or '{typeof(Task).FullName}<T>'.", ex.Message);
    }

    [Fact]
    public void ThrowsIfInterfaceMethodHasOutParam()
    {
        var clientProxy = new MockProxy();
        var ex = Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IOutParamMethodClient>.Build(clientProxy));
        Assert.Equal(
            $"Cannot generate proxy implementation for '{typeof(IOutParamMethodClient).FullName}.{nameof(IOutParamMethodClient.Method)}'. Client proxy methods must not have 'out' parameters.", ex.Message);
    }

    [Fact]
    public void ThrowsIfInterfaceMethodHasRefParam()
    {
        var clientProxy = new MockProxy();
        var ex = Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IRefParamMethodClient>.Build(clientProxy));
        Assert.Equal(
            $"Cannot generate proxy implementation for '{typeof(IRefParamMethodClient).FullName}.{nameof(IRefParamMethodClient.Method)}'. Client proxy methods must not have 'ref' parameters.", ex.Message);
    }

    [Fact]
    public void ThrowsIfInterfaceHasProperties()
    {
        var clientProxy = new MockProxy();
        var ex = Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IPropertiesClient>.Build(clientProxy));
        Assert.Equal("Type must not contain properties.", ex.Message);
    }

    [Fact]
    public void ThrowsIfInterfaceHasEvents()
    {
        var clientProxy = new MockProxy();
        var ex = Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IEventsClient>.Build(clientProxy));
        Assert.Equal("Type must not contain events.", ex.Message);
    }

    [Fact]
    public async Task ProducesImplementationThatProxiesMethodsToISingleClientProxyAsync()
    {
        var clientProxy = new MockSingleClientProxy();
        var typedProxy = TypedClientBuilder<ITestClient>.Build(clientProxy);

        var objArg = new object();
        var task = typedProxy.GetValue(1008, objArg, "test");
        Assert.False(task.IsCompleted);

        Assert.Collection(clientProxy.Sends,
            send =>
            {
                Assert.Equal("GetValue", send.Method);
                Assert.Collection(send.Arguments,
                    arg1 => Assert.Equal(1008, arg1),
                    arg2 => Assert.Same(objArg, arg2),
                    arg3 => Assert.Same("test", arg3));
                Assert.Equal(CancellationToken.None, send.CancellationToken);
                send.Complete();
            });

        var result = await task.DefaultTimeout();
        Assert.Equal(default(int), result);
    }

    [Fact]
    public async Task ThrowsIfReturnMethodUsedWithoutSingleClientProxy()
    {
        var clientProxy = new MockProxy();
        var typedProxy = TypedClientBuilder<ITestClient>.Build(clientProxy);

        var objArg = new object();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => typedProxy.GetValue(102, objArg, "test")).DefaultTimeout();
        Assert.Equal("InvokeAsync only works with Single clients.", ex.Message);

        Assert.Empty(clientProxy.Sends);
    }

    [Fact]
    public async Task ResultMethodSupportsCancellationToken()
    {
        var clientProxy = new MockSingleClientProxy();
        var typedProxy = TypedClientBuilder<ICancellationTokenMethod>.Build(clientProxy);
        CancellationTokenSource cts1 = new CancellationTokenSource();
        var task1 = typedProxy.MethodReturning("foo", cts1.Token);
        Assert.False(task1.IsCompleted);

        CancellationTokenSource cts2 = new CancellationTokenSource();
        var task2 = typedProxy.NoArgumentMethodReturning(cts2.Token);
        Assert.False(task2.IsCompleted);

        Assert.Collection(clientProxy.Sends,
            send1 =>
            {
                Assert.Equal("MethodReturning", send1.Method);
                Assert.Single(send1.Arguments);
                Assert.Collection(send1.Arguments,
                    arg1 => Assert.Equal("foo", arg1));
                Assert.Equal(cts1.Token, send1.CancellationToken);
                send1.Complete();
            },
            send2 =>
            {
                Assert.Equal("NoArgumentMethodReturning", send2.Method);
                Assert.Empty(send2.Arguments);
                Assert.Equal(cts2.Token, send2.CancellationToken);
                send2.Complete();
            });

        var result = await task1.DefaultTimeout();
        Assert.Equal(default(string), result);
        var result2 = await task2.DefaultTimeout();
        Assert.Equal(default(int), result2);
    }

    public interface ITestClient
    {
        Task Method(string arg1, int arg2, object arg3);
        Task<int> GetValue(int arg1, object arg2, string arg3);
    }

    public interface IRenamedTestClient
    {
        [HubMethodName("Method")]
        Task MethodAsync(string arg1, int arg2, object arg3);
    }

    public interface IVoidMethodClient
    {
        void Method(string arg1, int arg2, object arg3);
    }

    public interface IStringMethodClient
    {
        string Method(string arg1, int arg2, object arg3);
    }

    public interface IOutParamMethodClient
    {
        Task Method(out string arg1);
    }

    public interface IRefParamMethodClient
    {
        Task Method(ref string arg1);
    }

    public interface IInheritedClient : ITestClient
    {
        Task SubMethod(string foo);
    }

    public interface ICancellationTokenMethod
    {
        Task Method(string foo, CancellationToken cancellationToken);
        Task NoArgumentMethod(CancellationToken cancellationToken);

        Task<int> NoArgumentMethodReturning(CancellationToken cancellationToken);
        Task<string> MethodReturning(string foo, CancellationToken cancellationToken);
    }

    public interface IPropertiesClient
    {
        string Property { get; }
    }

    public interface IEventsClient
    {
        event EventHandler Event;
    }

    public interface ITestReturnValueClient
    {
        Task<int> GetValue();
    }

    private class MockProxy : IClientProxy
    {
        public IList<SendContext> Sends { get; } = new List<SendContext>();

        public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource();

            Sends.Add(new SendContext(method, args, cancellationToken, tcs));

            return tcs.Task;
        }
    }

    private class MockSingleClientProxy : ISingleClientProxy
    {
        public IList<SendContext> Sends { get; } = new List<SendContext>();

        public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource();

            Sends.Add(new SendContext(method, args, cancellationToken, tcs));

            return tcs.Task;
        }

        public async Task<T> InvokeCoreAsync<T>(string method, object[] args, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource();

            Sends.Add(new SendContext(method, args, cancellationToken, tcs));

            await tcs.Task;
            return default(T);
        }
    }

    private struct SendContext
    {
        private readonly TaskCompletionSource _tcs;

        public string Method { get; }
        public object[] Arguments { get; }
        public CancellationToken CancellationToken { get; }

        public SendContext(string method, object[] arguments, CancellationToken cancellationToken, TaskCompletionSource tcs) : this()
        {
            Method = method;
            Arguments = arguments;
            CancellationToken = cancellationToken;
            _tcs = tcs;
        }

        public void Complete()
        {
            _tcs.TrySetResult();
        }
    }
}
