// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

/// <summary>
/// Measures repeated dispatcher invocations with and without hub-method authorization.
/// </summary>
public class HubMethodAuthorizationBenchmark
{
    private const string InvocationPolicy = "InvokeHubMethod";
    private const string InvocationRole = "Invoker";
    private const string ExpectedResult = "Hello";

    private ServiceProvider _serviceProvider;
    private DefaultHubDispatcher<TestHub> _dispatcher;
    private DefaultConnectionContext _connection;
    private CompletionHubConnectionContext _connectionContext;
    private InvocationMessage _authorizedInvocation;
    private InvocationMessage _unprotectedInvocation;

    /// <summary>
    /// Gets or sets whether the policy provider permits caching composed policies.
    /// </summary>
    [Params(true, false)]
    public bool AllowsCachingPolicies { get; set; }

    /// <summary>
    /// Gets or sets whether policy-provider identities are reused across invocations.
    /// </summary>
    [Params(ServiceLifetime.Singleton, ServiceLifetime.Scoped)]
    public ServiceLifetime ProviderLifetime { get; set; }

    /// <summary>
    /// Creates a dispatcher, stable authorization metadata, and an authenticated connection.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Add(new ServiceDescriptor(typeof(IAuthorizationPolicyProvider), provider =>
            new BenchmarkAuthorizationPolicyProvider(provider.GetRequiredService<IOptions<AuthorizationOptions>>(), AllowsCachingPolicies),
            ProviderLifetime));
        services.AddSignalRCore();
        services.Configure<AuthorizationOptions>(options =>
        {
            options.AddPolicy(InvocationPolicy, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim("permission", "invoke"));
        });
        _serviceProvider = services.BuildServiceProvider();

        var lifetimeManager = _serviceProvider.GetRequiredService<HubLifetimeManager<TestHub>>();
        _dispatcher = new DefaultHubDispatcher<TestHub>(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            _serviceProvider.GetRequiredService<IHubContext<TestHub>>(),
            enableDetailedErrors: true,
            disableImplicitFromServiceParameters: true,
            NullLogger<DefaultHubDispatcher<TestHub>>.Instance,
            hubFilters: null,
            lifetimeManager);

        _connection = new DefaultConnectionContext("authorization-benchmark")
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "benchmark-user"),
                new Claim(ClaimTypes.Role, InvocationRole),
                new Claim("permission", "invoke"),
            ], authenticationType: "benchmark")),
        };
        _connectionContext = new CompletionHubConnectionContext(_connection)
        {
            Protocol = new DefaultHubDispatcherBenchmark.FakeHubProtocol(),
        };

        object[] arguments = [ExpectedResult];
        _authorizedInvocation = new InvocationMessage("1", nameof(TestHub.Authorized), arguments);
        _unprotectedInvocation = new InvocationMessage("1", nameof(TestHub.Unprotected), arguments);

        Invoke(_unprotectedInvocation);
        Invoke(_authorizedInvocation);
    }

    /// <summary>
    /// Invokes an unprotected method as a control for dispatcher overhead.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void Unprotected()
    {
        Invoke(_unprotectedInvocation);
    }

    /// <summary>
    /// Invokes a method requiring a named policy and role authorization.
    /// </summary>
    [Benchmark]
    public void Authorized()
    {
        Invoke(_authorizedInvocation);
    }

    private void Invoke(InvocationMessage invocation)
    {
        _connectionContext.Completion = null;
        _dispatcher.DispatchMessageAsync(_connectionContext, invocation).GetAwaiter().GetResult();

        // Dispatch can return before an invocation completes. The provider, handlers, hub methods,
        // and response writer are synchronous here; verify the response rather than measuring enqueueing.
        var completion = _connectionContext.Completion;
        if (completion is null || completion.Error is not null ||
            !completion.HasResult || completion.Result is not string result || result != ExpectedResult)
        {
            throw new InvalidOperationException($"Hub invocation did not complete successfully: {completion?.Error}");
        }
    }

    /// <summary>
    /// Releases the connection and all owned services.
    /// </summary>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        _connectionContext.Cleanup();
        await _connection.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private sealed class BenchmarkAuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> options,
        bool allowsCachingPolicies) : DefaultAuthorizationPolicyProvider(options)
    {
        public override bool AllowsCachingPolicies => allowsCachingPolicies;
    }

    private sealed class CompletionHubConnectionContext(ConnectionContext connection)
        : HubConnectionContext(connection, new HubConnectionContextOptions
        {
            KeepAliveInterval = Timeout.InfiniteTimeSpan,
            MaximumParallelInvocations = 1,
        }, NullLoggerFactory.Instance)
    {
        public CompletionMessage Completion { get; set; }

        public override ValueTask WriteAsync(HubMessage message, CancellationToken cancellationToken = default)
        {
            if (message is CompletionMessage completion)
            {
                Completion = completion;
            }

            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Provides equivalent synchronous methods with and without authorization metadata.
    /// </summary>
    public class TestHub : Hub
    {
        /// <summary>
        /// Returns the supplied value after the dispatcher authorizes the invocation.
        /// </summary>
        [Authorize(Policy = InvocationPolicy)]
        [Authorize(Roles = InvocationRole)]
        public string Authorized(string value)
        {
            return value;
        }

        /// <summary>
        /// Returns the supplied value without requiring authorization.
        /// </summary>
        public string Unprotected(string value)
        {
            return value;
        }
    }
}
