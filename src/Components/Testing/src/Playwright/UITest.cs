// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;

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
public abstract class UITest
{
    /// <summary>
    /// Servers whose captured stdout/stderr should be attached to the test result when the
    /// test fails. Add the servers a test starts here (typically from an
    /// <see cref="InitializeCoreAsync"/> override); the generated cleanup hook reads this list.
    /// </summary>
    protected internal List<ServerInstance> DiagnosticServers { get; } = new();

    /// <summary>
    /// Per-test initialization. Override to perform setup, calling
    /// <c>await base.InitializeCoreAsync()</c> first.
    /// </summary>
    protected internal virtual Task InitializeCoreAsync() => Task.CompletedTask;

    /// <summary>
    /// Per-test cleanup. Override to perform teardown; the base chain runs last.
    /// </summary>
    protected internal virtual Task CleanupCoreAsync() => Task.CompletedTask;
}
