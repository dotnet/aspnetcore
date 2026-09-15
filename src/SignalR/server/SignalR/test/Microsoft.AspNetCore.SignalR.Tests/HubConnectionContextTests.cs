// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class HubConnectionContextTests
{
    [Fact]
    public async Task UnknownCancellationAndCleanupDoNotAllocateCancellationSources()
    {
        await using var transport = new DefaultConnectionContext();
        var connection = HubConnectionContextUtils.Create(transport);
        using var services = (ServiceProvider)HubConnectionHandlerTestUtils.CreateServiceProvider();
        var dispatcher = new DefaultHubDispatcher<MethodHub>(
            services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<IHubContext<MethodHub>>(),
            enableDetailedErrors: false,
            disableImplicitFromServiceParameters: false,
            NullLogger<DefaultHubDispatcher<MethodHub>>.Instance,
            hubFilters: null,
            services.GetRequiredService<HubLifetimeManager<MethodHub>>());

        Assert.Null(GetCancellationSources(connection));
        Assert.False(connection.TryGetActiveRequestCancellationSource("missing", out var cancellationSource));
        Assert.Null(cancellationSource);
        await dispatcher.DispatchMessageAsync(connection, new CancelInvocationMessage("missing")).DefaultTimeout();
        Assert.Null(GetCancellationSources(connection));

        connection.Cleanup();
        Assert.Null(GetCancellationSources(connection));
    }

    [Fact]
    public async Task ConcurrentFirstRegistrationsShareCancellationSources()
    {
        const int registrationCount = 16;
        await using var transport = new DefaultConnectionContext();
        var connection = HubConnectionContextUtils.Create(transport);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readyCount = 0;
        using var cancellationSource = new CancellationTokenSource();
        Assert.Null(GetCancellationSources(connection));

        var registrations = Enumerable.Range(0, registrationCount).Select(index => Task.Run(async () =>
        {
            if (Interlocked.Increment(ref readyCount) == registrationCount)
            {
                ready.SetResult();
            }

            await start.Task;
            var sources = connection.ActiveRequestCancellationSources;
            Assert.True(sources.TryAdd(index.ToString(CultureInfo.InvariantCulture), cancellationSource));
            return sources;
        })).ToArray();
        try
        {
            await ready.Task.DefaultTimeout();
            Assert.Null(GetCancellationSources(connection));
        }
        finally
        {
            start.TrySetResult();
        }

        var dictionaries = await Task.WhenAll(registrations).DefaultTimeout();
        var published = connection.ActiveRequestCancellationSources;
        Assert.Same(published, GetCancellationSources(connection));
        Assert.All(dictionaries, dictionary => Assert.Same(published, dictionary));
        Assert.Equal(registrationCount, published.Count);
        Assert.True(connection.TryGetActiveRequestCancellationSource("0", out var registeredSource));
        Assert.Same(cancellationSource, registeredSource);

        Assert.True(published.TryAdd("case", cancellationSource));
        Assert.True(published.TryAdd("CASE", cancellationSource));
        connection.Cleanup();
    }

    internal static ConcurrentDictionary<string, CancellationTokenSource>? GetCancellationSources(HubConnectionContext connection)
    {
        var field = typeof(HubConnectionContext).GetField("_activeRequestCancellationSources", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (ConcurrentDictionary<string, CancellationTokenSource>?)field.GetValue(connection);
    }
}
