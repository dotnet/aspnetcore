// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

[AttributeUsage(AttributeTargets.Method)]
internal class HubClientProxyAttribute : Attribute
{

}

internal static partial class RegisterCallbackProviderExtensions
{
    [HubClientProxy]
    public static partial IDisposable SetHubClient<T>(this HubConnection conn, T p);
}

public class HubClientProxyGeneratorTests
{
    public interface IMyClient
    {
        void NoArg();
        void SingleArg(int a);
        void ManyArgs(int a, float b, int? c);
        Task ReturnTask();
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

        public List<(int, float, int?)> CallsOfManyArgs = new();
        public void ManyArgs(int a, float b, int? c)
        {
            CallsOfManyArgs.Add((a, b, c));
        }

        public int CallsOfReturnTask;
        public Task ReturnTask()
        {
            CallsOfReturnTask += 1;
            return Task.CompletedTask;
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
        var mockConn = MockHubConnection.Get();
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
                It.Is<Type[]>(t => t.Length == 3 && t[0] == typeof(int) && t[1] == typeof(float) && t[2] == typeof(int?)),
                It.IsAny<Func<object[], object, Task>>(),
                It.IsAny<object>()))
            .Returns(manyArgsReg);
        var returnTaskReg = new Disposable();
        mockConn
            .Setup(x => x.On(
                "ReturnTask",
                Array.Empty<Type>(),
                It.IsAny<Func<object[], object, Task>>(),
                It.IsAny<object>()))
            .Returns(returnTaskReg);
        var conn = mockConn.Object;
        var myClient = new MyClient();

        // Act
        var registration = conn.SetHubClient<IMyClient>(myClient);

        // Assert
        mockConn.VerifyAll();
        Assert.False(noArgReg.IsDisposed);
        Assert.False(singleArgReg.IsDisposed);
        Assert.False(manyArgsReg.IsDisposed);
        Assert.False(returnTaskReg.IsDisposed);
    }

    [Fact]
    public void UnregistersCallbackProvider()
    {
        // Arrange
        var mockConn = MockHubConnection.Get();
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
                It.Is<Type[]>(t => t.Length == 3 && t[0] == typeof(int) && t[1] == typeof(float) && t[2] == typeof(int?)),
                It.IsAny<Func<object[], object, Task>>(),
                It.IsAny<object>()))
            .Returns(manyArgsReg);
        var returnTaskReg = new Disposable();
        mockConn
            .Setup(x => x.On(
                "ReturnTask",
                Array.Empty<Type>(),
                It.IsAny<Func<object[], object, Task>>(),
                It.IsAny<object>()))
            .Returns(returnTaskReg);
        var conn = mockConn.Object;
        var myClient = new MyClient();
        var registration = conn.SetHubClient<IMyClient>(myClient);

        // Act
        registration.Dispose();

        // Assert
        Assert.True(noArgReg.IsDisposed);
        Assert.True(singleArgReg.IsDisposed);
        Assert.True(manyArgsReg.IsDisposed);
        Assert.True(returnTaskReg.IsDisposed);
    }

    [Fact]
    public async Task CallbacksGetTriggered()
    {
        // Arrange
        var mockConn = MockHubConnection.Get();
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
                It.Is<Type[]>(t => t.Length == 3 && t[0] == typeof(int) && t[1] == typeof(float) && t[2] == typeof(int?)),
                It.IsAny<Func<object[], object, Task>>(),
                It.IsAny<object>()))
            .Callback(
                (string methodName, Type[] parameterTypes, Func<object[], object, Task> handler, object state) =>
                {
                    manyArgsFunc = handler;
                    manyArgsState = state;
                })
            .Returns(manyArgsReg);
        var returnTaskReg = new Disposable();
        Func<object[], object, Task> returnTaskFunc = null;
        object returnTaskState = null;
        mockConn
            .Setup(x => x.On(
                "ReturnTask",
                Array.Empty<Type>(),
                It.IsAny<Func<object[], object, Task>>(),
                It.IsAny<object>()))
            .Callback(
                (string methodName, Type[] parameterTypes, Func<object[], object, Task> handler, object state) =>
                {
                    returnTaskFunc = handler;
                    returnTaskState = state;
                })
            .Returns(returnTaskReg);
        var conn = mockConn.Object;
        var myClient = new MyClient();
        var registration = conn.SetHubClient<IMyClient>(myClient);

        // Act + Assert
        Assert.NotNull(noArgFunc);
        await noArgFunc(Array.Empty<object>(), noArgState);
        Assert.Equal(1, myClient.CallsOfNoArg);

        Assert.NotNull(singleArgFunc);
        await singleArgFunc(new object[] { 10 }, singleArgState);
        Assert.Single(myClient.CallsOfSingleArg);
        Assert.Equal(10, myClient.CallsOfSingleArg[0]);

        Assert.NotNull(manyArgsFunc);
        await singleArgFunc(new object[] { 10, 5.5f, null }, manyArgsState);
        Assert.Single(myClient.CallsOfManyArgs);
        Assert.Equal((10, 5.5f, null), myClient.CallsOfManyArgs[0]);

        Assert.NotNull(returnTaskFunc);
        await returnTaskFunc(Array.Empty<object>(), returnTaskState);
        Assert.Equal(1, myClient.CallsOfReturnTask);
    }
}
