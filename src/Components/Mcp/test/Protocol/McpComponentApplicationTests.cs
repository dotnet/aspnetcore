// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Mcp.Protocol;
using ModelContextProtocol;

namespace Microsoft.AspNetCore.Components.Mcp;

public class McpComponentApplicationTests
{
    [Fact]
    public async Task Association_UsesTheCurrentPrincipalAndOpaqueViewCapability()
    {
        var application = CreateApplication("first");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "owner"), new Claim(ClaimTypes.Role, "editor")], "test"));
        var view = await application.CreateViewAsync(principal, McpComponentParameters.Empty);
        var equivalentPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "editor"), new Claim(ClaimTypes.NameIdentifier, "owner")], "test"));
        var otherPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "other")], "test"));

        Assert.Same(view, application.GetView(view.Id, equivalentPrincipal));
        Assert.Throws<McpException>(() => application.GetView(view.Id, otherPrincipal));
        Assert.Throws<McpException>(() => CreateApplication("second").GetView(view.Id, principal));
        await application.RemoveViewAsync(view);
        Assert.Throws<McpException>(() => application.GetView(view.Id, principal));
    }

    [Fact]
    public async Task AnonymousAssociation_DoesNotInventPhysicalClientIdentity()
    {
        var application = CreateApplication("anonymous");
        var view = await application.CreateViewAsync(new ClaimsPrincipal(new ClaimsIdentity()), McpComponentParameters.Empty);

        Assert.Same(view, application.GetView(view.Id, new ClaimsPrincipal(new ClaimsIdentity())));
        await application.RemoveViewAsync(view);
    }

    [Fact]
    public async Task Close_CancelsButWaitsForOutstandingWork()
    {
        var view = new McpComponentApplication.View("owner", McpComponentParameters.Empty);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observed = default;
        var operation = view.RunAsync(async cancellationToken =>
        {
            observed = cancellationToken;
            started.SetResult();
            await release.Task;
            cancellationToken.ThrowIfCancellationRequested();
            return "finished";
        }, CancellationToken.None);
        await started.Task;
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = observed.Register(() => cancellationObserved.TrySetResult());

        var close = view.CloseAsync();
        await cancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(observed.IsCancellationRequested);
        Assert.False(operation.IsCompleted);
        Assert.False(close.IsCompleted);
        release.SetResult();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        await close;
        await Assert.ThrowsAsync<McpException>(() => view.RunAsync(_ => Task.FromResult("late"), CancellationToken.None));
        Assert.Same(close, view.CloseAsync());
    }

    [Fact]
    public async Task OperationFailure_DoesNotRetainACompletedOperation()
    {
        var view = new McpComponentApplication.View("owner", McpComponentParameters.Empty);
        var expected = new InvalidOperationException("handler-failure");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => view.RunAsync<string>(_ => throw expected, CancellationToken.None));

        Assert.Same(expected, exception);
        Assert.False(view.HasActiveOperations);
        await view.CloseAsync();
    }

    private static McpComponentApplication CreateApplication(string name)
        => new(name, typeof(TestComponent), new Uri("https://example.test/"), new Uri("https://example.test/form"));

    private sealed class TestComponent : ComponentBase;
}
