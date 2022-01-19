// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class DefaultActionDescriptorCollectionProviderTest
{
    [Fact]
    public void ActionDescriptors_ReadsDescriptorsFromActionDescriptorProviders()
    {
        // Arrange
        var expected1 = new ActionDescriptor();
        var actionDescriptorProvider1 = GetActionDescriptorProvider(expected1);

        var expected2 = new ActionDescriptor();
        var expected3 = new ActionDescriptor();
        var actionDescriptorProvider2 = GetActionDescriptorProvider(expected2, expected3);

        var actionDescriptorCollectionProvider = new DefaultActionDescriptorCollectionProvider(
            new[] { actionDescriptorProvider1, actionDescriptorProvider2 },
            Enumerable.Empty<IActionDescriptorChangeProvider>(),
            NullLogger<DefaultActionDescriptorCollectionProvider>.Instance);

        // Act
        var collection = actionDescriptorCollectionProvider.ActionDescriptors;

        // Assert
        Assert.Equal(0, collection.Version);
        Assert.Collection(
            collection.Items,
            descriptor => Assert.Same(expected1, descriptor),
            descriptor => Assert.Same(expected2, descriptor),
            descriptor => Assert.Same(expected3, descriptor));
    }

    [Fact]
    public void ActionDescriptors_CachesValuesByDefault()
    {
        // Arrange
        var actionDescriptorProvider = GetActionDescriptorProvider(new ActionDescriptor());

        var actionDescriptorCollectionProvider = new DefaultActionDescriptorCollectionProvider(
            new[] { actionDescriptorProvider },
            Enumerable.Empty<IActionDescriptorChangeProvider>(),
            NullLogger<DefaultActionDescriptorCollectionProvider>.Instance);

        // Act - 1
        var collection1 = actionDescriptorCollectionProvider.ActionDescriptors;

        // Assert - 1
        Assert.Equal(0, collection1.Version);

        // Act - 2
        var collection2 = actionDescriptorCollectionProvider.ActionDescriptors;

        // Assert - 2
        Assert.Same(collection1, collection2);
        Mock.Get(actionDescriptorProvider)
            .Verify(v => v.OnProvidersExecuting(It.IsAny<ActionDescriptorProviderContext>()), Times.Once());
    }

    [Fact]
    public void ActionDescriptors_UpdatesAndResubscribes_WhenChangeTokenTriggers()
    {
        // Arrange
        var actionDescriptorProvider = new Mock<IActionDescriptorProvider>();
        var expected1 = new ActionDescriptor();
        var expected2 = new ActionDescriptor();
        var expected3 = new ActionDescriptor();

        var invocations = 0;
        actionDescriptorProvider
            .Setup(p => p.OnProvidersExecuting(It.IsAny<ActionDescriptorProviderContext>()))
            .Callback((ActionDescriptorProviderContext context) =>
            {
                if (invocations == 0)
                {
                    context.Results.Add(expected1);
                }
                else if (invocations == 1)
                {
                    context.Results.Add(expected2);
                }
                else
                {
                    context.Results.Add(expected3);
                }

                invocations++;
            });
        var changeProvider = new TestChangeProvider();
        var actionDescriptorCollectionProvider = new DefaultActionDescriptorCollectionProvider(
            new[] { actionDescriptorProvider.Object },
            new[] { changeProvider },
            NullLogger<DefaultActionDescriptorCollectionProvider>.Instance);

        // Act - 1
        var changeToken1 = actionDescriptorCollectionProvider.GetChangeToken();
        var collection1 = actionDescriptorCollectionProvider.ActionDescriptors;

        ActionDescriptorCollection captured = null;
        changeToken1.RegisterChangeCallback((_) =>
        {
            captured = actionDescriptorCollectionProvider.ActionDescriptors;
        }, null);

        // Assert - 1
        Assert.False(changeToken1.HasChanged);
        Assert.Equal(0, collection1.Version);
        Assert.Collection(collection1.Items,
            item => Assert.Same(expected1, item));

        // Act - 2
        changeProvider.TokenSource.Cancel();
        var changeToken2 = actionDescriptorCollectionProvider.GetChangeToken();
        var collection2 = actionDescriptorCollectionProvider.ActionDescriptors;

        changeToken2.RegisterChangeCallback((_) =>
        {
            captured = actionDescriptorCollectionProvider.ActionDescriptors;
        }, null);

        // Assert - 2
        Assert.NotSame(changeToken1, changeToken2);
        Assert.True(changeToken1.HasChanged);
        Assert.False(changeToken2.HasChanged);

        Assert.NotSame(collection1, collection2);
        Assert.NotNull(captured);
        Assert.Same(captured, collection2);
        Assert.Equal(1, collection2.Version);
        Assert.Collection(collection2.Items,
            item => Assert.Same(expected2, item));

        // Act - 3
        changeProvider.TokenSource.Cancel();
        var changeToken3 = actionDescriptorCollectionProvider.GetChangeToken();
        var collection3 = actionDescriptorCollectionProvider.ActionDescriptors;

        // Assert - 3
        Assert.NotSame(changeToken2, changeToken3);
        Assert.True(changeToken2.HasChanged);
        Assert.False(changeToken3.HasChanged);

        Assert.NotSame(collection2, collection3);
        Assert.NotNull(captured);
        Assert.Same(captured, collection3);
        Assert.Equal(2, collection3.Version);
        Assert.Collection(collection3.Items,
            item => Assert.Same(expected3, item));
    }

    private static IActionDescriptorProvider GetActionDescriptorProvider(params ActionDescriptor[] values)
    {
        var actionDescriptorProvider = new Mock<IActionDescriptorProvider>();
        actionDescriptorProvider
            .Setup(p => p.OnProvidersExecuting(It.IsAny<ActionDescriptorProviderContext>()))
            .Callback((ActionDescriptorProviderContext context) =>
            {
                foreach (var value in values)
                {
                    context.Results.Add(value);
                }
            });

        return actionDescriptorProvider.Object;
    }

    private class TestChangeProvider : IActionDescriptorChangeProvider
    {
        public CancellationTokenSource TokenSource { get; private set; }

        public IChangeToken GetChangeToken()
        {
            TokenSource = new CancellationTokenSource();
            return new CancellationChangeToken(TokenSource.Token);
        }
    }
}
