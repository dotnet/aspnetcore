// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class PageLoaderMatcherPolicyTest
{
    [Fact]
    public async Task ApplyAsync_UpdatesCandidateSet()
    {
        // Arrange
        var compiled = new CompiledPageActionDescriptor();
        compiled.Endpoint = CreateEndpoint(new PageActionDescriptor());

        var candidateSet = CreateCandidateSet(compiled);

        var loader = Mock.Of<PageLoader>(p => p.LoadAsync(It.IsAny<PageActionDescriptor>(), It.IsAny<EndpointMetadataCollection>()) == Task.FromResult(compiled));
        var policy = new PageLoaderMatcherPolicy(loader);

        // Act
        await policy.ApplyAsync(new DefaultHttpContext(), candidateSet);

        // Assert
        Assert.Same(compiled.Endpoint, candidateSet[0].Endpoint);
    }

    [Fact]
    public async Task ApplyAsync_ReadsLoaderFromRequestServices()
    {
        // Arrange
        var compiled = new CompiledPageActionDescriptor();
        compiled.Endpoint = CreateEndpoint(new PageActionDescriptor());

        var candidateSet = CreateCandidateSet(compiled);
        var loader = new Mock<PageLoader>();
        loader.Setup(l => l.LoadAsync(It.IsAny<PageActionDescriptor>(), It.IsAny<EndpointMetadataCollection>()))
            .Returns(Task.FromResult(compiled))
            .Verifiable();
        var policy = new PageLoaderMatcherPolicy();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(loader.Object).BuildServiceProvider(),
        };

        // Act
        await policy.ApplyAsync(httpContext, candidateSet);

        // Assert
        Assert.Same(compiled.Endpoint, candidateSet[0].Endpoint);
        loader.Verify();
    }

    [Fact]
    public async Task ApplyAsync_UpdatesCandidateSet_IfLoaderReturnsAsynchronously()
    {
        // Arrange
        var compiled = new CompiledPageActionDescriptor();
        compiled.Endpoint = CreateEndpoint(new PageActionDescriptor());

        var tcs = new TaskCompletionSource();
        var candidateSet = CreateCandidateSet(compiled);

        var loadTask = Task.Run(async () =>
        {
            await tcs.Task;
            return compiled;
        });
        var loader = Mock.Of<PageLoader>(p => p.LoadAsync(It.IsAny<PageActionDescriptor>(), It.IsAny<EndpointMetadataCollection>()) == loadTask);
        var policy = new PageLoaderMatcherPolicy(loader);

        // Act
        var applyTask = policy.ApplyAsync(new DefaultHttpContext(), candidateSet);
        tcs.SetResult();
        await applyTask;

        // Assert
        Assert.Same(compiled.Endpoint, candidateSet[0].Endpoint);
    }

    private static Endpoint CreateEndpoint(ActionDescriptor action)
    {
        var metadata = new List<object>() { action, };
        return new Endpoint(
            (context) => Task.CompletedTask,
            new EndpointMetadataCollection(metadata),
            $"test: {action?.DisplayName}");
    }

    private static CandidateSet CreateCandidateSet(params ActionDescriptor[] actions)
    {
        var values = new RouteValueDictionary[actions.Length];
        for (var i = 0; i < actions.Length; i++)
        {
            values[i] = new RouteValueDictionary();
        }

        var candidateSet = new CandidateSet(
            actions.Select(CreateEndpoint).ToArray(),
            values,
            new int[actions.Length]);
        return candidateSet;
    }
}
