// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class HubConnectionHandlerTestUtils
{
    public static Type GetConnectionHandlerType(Type hubType)
    {
        var connectionHandlerType = typeof(HubConnectionHandler<>);
        return connectionHandlerType.MakeGenericType(hubType);
    }

    public static Type GetGenericType(Type genericType, Type hubType)
    {
        return genericType.MakeGenericType(hubType);
    }

    public static void AssertHubMessage(HubMessage expected, HubMessage actual)
    {
        // We aren't testing InvocationIds here
        switch (expected)
        {
            case CompletionMessage expectedCompletion:
                var actualCompletion = Assert.IsType<CompletionMessage>(actual);
                Assert.Equal(expectedCompletion.Error, actualCompletion.Error);
                Assert.Equal(expectedCompletion.HasResult, actualCompletion.HasResult);
                Assert.Equal(expectedCompletion.Result, actualCompletion.Result);
                break;
            case StreamItemMessage expectedStreamItem:
                var actualStreamItem = Assert.IsType<StreamItemMessage>(actual);
                Assert.Equal(expectedStreamItem.Item, actualStreamItem.Item);
                break;
            case InvocationMessage expectedInvocation:
                var actualInvocation = Assert.IsType<InvocationMessage>(actual);

                // Either both must have non-null invocationIds or both must have null invocation IDs. Checking the exact value is NOT desired here though as it could be randomly generated
                Assert.True((expectedInvocation.InvocationId == null && actualInvocation.InvocationId == null) ||
                    (expectedInvocation.InvocationId != null && actualInvocation.InvocationId != null));
                Assert.Equal(expectedInvocation.Target, actualInvocation.Target);
                Assert.Equal(expectedInvocation.Arguments, actualInvocation.Arguments);
                break;
            default:
                throw new InvalidOperationException($"Unsupported Hub Message type {expected.GetType()}");
        }
    }

    public static IServiceProvider CreateServiceProvider(Action<ServiceCollection> addServices = null, ILoggerFactory loggerFactory = null)
    {
        var services = new ServiceCollection();
        services.AddOptions()
            .AddLogging();

        services.AddSignalR()
            .AddMessagePackProtocol();

        addServices?.Invoke(services);

        if (loggerFactory != null)
        {
            services.AddSingleton(loggerFactory);
        }

        return services.BuildServiceProvider();
    }

    public static Connections.ConnectionHandler GetHubConnectionHandler(Type hubType, ILoggerFactory loggerFactory = null, Action<ServiceCollection> addServices = null)
    {
        var serviceProvider = CreateServiceProvider(addServices, loggerFactory);
        return (Connections.ConnectionHandler)serviceProvider.GetService(GetConnectionHandlerType(hubType));
    }
}

public class Result
{
    public string Message { get; set; }
#pragma warning disable IDE1006 // Naming Styles
    // testing casing
    public string paramName { get; set; }
#pragma warning restore IDE1006 // Naming Styles
}

public class TrackDispose
{
    public int DisposeCount = 0;
}

public class AsyncDisposable : IAsyncDisposable
{
    private readonly TrackDispose _trackDispose;

    public AsyncDisposable(TrackDispose trackDispose)
    {
        _trackDispose = trackDispose;
    }

    public ValueTask DisposeAsync()
    {
        _trackDispose.DisposeCount++;
        return default;
    }
}
