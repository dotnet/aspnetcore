// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Testing.Playwright;

/// <summary>
/// Root base class for E2E UI test classes. It owns the framework-agnostic per-test
/// lifecycle (<see cref="InitializeCoreAsync"/> / <see cref="CleanupCoreAsync"/>) and the
/// set of servers whose captured output should be attached to a failing test.
/// </summary>
/// <remarks>
/// <para>
/// This type deliberately has no dependency on any test framework. A test class opts in
/// to the E2E infrastructure by deriving from a capability base (<see cref="PlaywrightTest"/>,
/// <see cref="BrowserTest"/>, <see cref="ContextTest"/>, or <see cref="PageTest"/>) and
/// applying <see cref="UITestAttribute"/>. The bundled source generator then emits the
/// test-framework binding (for MSTest: <c>[TestClass]</c>, the <c>TestContext</c> property,
/// and the <c>[TestInitialize]</c>/<c>[TestCleanup]</c> hooks) onto the consumer's
/// <c>partial</c> class, calling the lifecycle methods below in the correct base-first order.
/// </para>
/// <para>
/// Derived classes override <see cref="InitializeCoreAsync"/> / <see cref="CleanupCoreAsync"/>
/// and <b>must call the base implementation first</b> so the capability ladder (browser →
/// context → page) is established before per-test setup runs.
/// </para>
/// </remarks>
public abstract class UITest : IAsyncDisposable
{
    private readonly object _disposablesLock = new();
    private readonly List<IAsyncDisposable> _disposables = new();
    private readonly List<ServerInstance> _diagnosticServers = new();

    internal ITestArtifactManager ArtifactManager
        => this as ITestArtifactManager
            ?? throw new InvalidOperationException(
                $"{GetType().FullName} must be annotated with UITestAttribute.");

    internal T RegisterForDisposal<T>(T disposable) where T : IAsyncDisposable
    {
        ArgumentNullException.ThrowIfNull(disposable);
        lock (_disposablesLock)
        {
            _disposables.Add(disposable);
        }

        return disposable;
    }

    /// <summary>
    /// Starts an application server and automatically captures its output when startup or the test fails.
    /// </summary>
    /// <typeparam name="TApp">Any public type from the application assembly.</typeparam>
    /// <param name="factory">The assembly-scoped server factory.</param>
    /// <param name="configure">Optional server-start configuration.</param>
    /// <returns>The running server instance.</returns>
    protected async Task<ServerInstance> StartServerAsync<TApp>(
        ServerFactory factory,
        Action<ServerStartOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var appName = typeof(TApp).Assembly.GetName().Name
            ?? throw new InvalidOperationException(
                $"Could not determine assembly name for type '{typeof(TApp).FullName}'.");
        var server = await factory.StartServerAsync(appName, configure, ArtifactManager).ConfigureAwait(false);
        lock (_disposablesLock)
        {
            _diagnosticServers.Add(server);
        }

        return server;
    }

    /// <summary>
    /// Starts tracing an existing browser context until the current test is disposed.
    /// </summary>
    /// <param name="context">The browser context to trace.</param>
    protected async Task TraceAsync(IBrowserContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var directory = ArtifactManager.CreateArtifactDirectory("browser");
        var session = await PlaywrightExtensions.TraceAsync(context, directory, ArtifactManager).ConfigureAwait(false);
        RegisterForDisposal(session);
    }

    /// <summary>
    /// Per-test initialization. Override to perform setup, calling
    /// <c>await base.InitializeCoreAsync()</c> first.
    /// </summary>
    protected internal virtual Task InitializeCoreAsync() => Task.CompletedTask;

    /// <summary>
    /// Per-test cleanup. Override to perform teardown; the base chain runs last.
    /// </summary>
    protected internal virtual Task CleanupCoreAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        IAsyncDisposable[] disposables;
        ServerInstance[] diagnosticServers;
        lock (_disposablesLock)
        {
            disposables = _disposables.ToArray();
            _disposables.Clear();
            diagnosticServers = _diagnosticServers.ToArray();
            _diagnosticServers.Clear();
        }

        List<Exception>? exceptions = null;
        try
        {
            if (ArtifactManager.ShouldSaveArtifacts() && diagnosticServers.Length > 0)
            {
                try
                {
                    var directory = ArtifactManager.CreateArtifactDirectory("server-output");
                    var paths = diagnosticServers
                        .SelectMany(server => server.WriteCapturedOutputTo(directory))
                        .ToArray();
                    ArtifactManager.AddArtifacts(paths);
                }
                catch (Exception exception)
                {
                    exceptions ??= new();
                    exceptions.Add(exception);
                }
            }

            for (var i = disposables.Length - 1; i >= 0; i--)
            {
                try
                {
                    await disposables[i].DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    exceptions ??= new();
                    exceptions.Add(exception);
                }
            }
        }
        finally
        {
            GC.SuppressFinalize(this);
        }

        if (exceptions is not null)
        {
            throw new AggregateException(exceptions);
        }
    }
}
