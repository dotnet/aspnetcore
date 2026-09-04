// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Hosting;

internal static class HostInitializerKey
{
    public const string Static = "Static";
    public const string Server = "Server";
}

internal sealed class HostInitializerCollection
{
    private readonly ImmutableArray<IHostInitializer> _sharedInitializers;
    private readonly ImmutableArray<IHostInitializer> _staticInitializers;
    private readonly ImmutableArray<IHostInitializer> _serverInitializers;

    public HostInitializerCollection(IServiceProvider services)
    {
        var sharedInitializers = GetOrderedInitializers(services.GetServices<IHostInitializer>());
        var staticInitializers = GetOrderedInitializers(
            services.GetKeyedServices<IHostInitializer>(HostInitializerKey.Static));
        var serverInitializers = GetOrderedInitializers(
            services.GetKeyedServices<IHostInitializer>(HostInitializerKey.Server));

        _sharedInitializers = CreateEffectiveSet(sharedInitializers, []);
        _staticInitializers = CreateEffectiveSet(sharedInitializers, staticInitializers);
        _serverInitializers = CreateEffectiveSet(sharedInitializers, serverInitializers);
    }

    public HostInitializerInvoker GetInitializerInvoker(IServiceProvider services)
        => new(_sharedInitializers, services);

    public HostInitializerInvoker GetInitializerInvoker(IServiceProvider services, string key)
        => new(key switch
        {
            HostInitializerKey.Static => _staticInitializers,
            HostInitializerKey.Server => _serverInitializers,
            _ => throw new ArgumentException($"Unknown host initializer key '{key}'.", nameof(key)),
        }, services);

    private static ImmutableArray<OrderedInitializer> GetOrderedInitializers(
        IEnumerable<IHostInitializer> initializers)
        => initializers
            .Select(static initializer => new OrderedInitializer(initializer, initializer.Order))
            .ToImmutableArray();

    private static ImmutableArray<IHostInitializer> CreateEffectiveSet(
        ImmutableArray<OrderedInitializer> sharedInitializers,
        ImmutableArray<OrderedInitializer> keyedInitializers)
    {
        var initializers = sharedInitializers.AddRange(keyedInitializers);
        var orderedInitializers = initializers.Sort(
            static (left, right) => left.Order.CompareTo(right.Order));

        for (var i = 1; i < orderedInitializers.Length; i++)
        {
            var previous = orderedInitializers[i - 1];
            var current = orderedInitializers[i];
            if (previous.Order == current.Order)
            {
                throw new InvalidOperationException(
                    $"Host initializers '{previous.Initializer.GetType()}' and '{current.Initializer.GetType()}' " +
                    $"both use Order '{current.Order}'. Order values must be unique within each host.");
            }
        }

        return orderedInitializers
            .Select(static initializer => initializer.Initializer)
            .ToImmutableArray();
    }

    private readonly record struct OrderedInitializer(IHostInitializer Initializer, int Order);
}

internal sealed class HostInitializerInvoker(
    ImmutableArray<IHostInitializer> initializers,
    IServiceProvider services)
{
    private Task? _hostInitializationTask;
    private Task? _browserInitializationTask;

    public Task InitializeHostAsync(CancellationToken cancellationToken = default)
        => _hostInitializationTask ??= InitializeHostCoreAsync(cancellationToken);

    public Task InitializeBrowserAsync(CancellationToken cancellationToken = default)
        => _browserInitializationTask ??= InitializeBrowserCoreAsync(cancellationToken);

    private async Task InitializeHostCoreAsync(CancellationToken cancellationToken)
    {
        foreach (var initializer in initializers)
        {
            await initializer.InitializeHostAsync(services, cancellationToken);
        }
    }

    private async Task InitializeBrowserCoreAsync(CancellationToken cancellationToken)
    {
        await InitializeHostAsync(cancellationToken);

        foreach (var initializer in initializers)
        {
            await initializer.InitializeBrowserAsync(services, cancellationToken);
        }
    }
}
