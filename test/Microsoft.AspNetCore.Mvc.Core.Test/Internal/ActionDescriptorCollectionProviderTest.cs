// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ActionDescriptorCollectionProviderTest
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

            var actionDescriptorCollectionProvider = new ActionDescriptorCollectionProvider(
                new[] { actionDescriptorProvider1, actionDescriptorProvider2 },
                Enumerable.Empty<IActionDescriptorChangeProvider>());

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

            var actionDescriptorCollectionProvider = new ActionDescriptorCollectionProvider(
                new[] { actionDescriptorProvider },
                Enumerable.Empty<IActionDescriptorChangeProvider>());

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
        public void ActionDescriptors_UpdateWhenChangeTokenProviderChanges()
        {
            // Arrange
            var actionDescriptorProvider = new Mock<IActionDescriptorProvider>();
            var expected1 = new ActionDescriptor();
            var expected2 = new ActionDescriptor();

            var invocations = 0;
            actionDescriptorProvider
                .Setup(p => p.OnProvidersExecuting(It.IsAny<ActionDescriptorProviderContext>()))
                .Callback((ActionDescriptorProviderContext context) =>
                {
                    if (invocations == 0)
                    {
                        context.Results.Add(expected1);
                    }
                    else
                    {
                        context.Results.Add(expected2);
                    }

                    invocations++;
                });
            var changeProvider = new TestChangeProvider();
            var actionDescriptorCollectionProvider = new ActionDescriptorCollectionProvider(
                new[] { actionDescriptorProvider.Object },
                new[] { changeProvider });

            // Act - 1
            var collection1 = actionDescriptorCollectionProvider.ActionDescriptors;

            // Assert - 1
            Assert.Equal(0, collection1.Version);
            Assert.Collection(collection1.Items,
                item => Assert.Same(expected1, item));

            // Act - 2
            changeProvider.TokenSource.Cancel();
            var collection2 = actionDescriptorCollectionProvider.ActionDescriptors;

            // Assert - 2
            Assert.NotSame(collection1, collection2);
            Assert.Equal(1, collection2.Version);
            Assert.Collection(collection2.Items,
                item => Assert.Same(expected2, item));
        }

        [Fact]
        public void ActionDescriptors_SubscribesToNewChangeNotificationsAfterInvalidating()
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
            var actionDescriptorCollectionProvider = new ActionDescriptorCollectionProvider(
                new[] { actionDescriptorProvider.Object },
                new[] { changeProvider });

            // Act - 1
            var collection1 = actionDescriptorCollectionProvider.ActionDescriptors;

            // Assert - 1
            Assert.Equal(0, collection1.Version);
            Assert.Collection(collection1.Items,
                item => Assert.Same(expected1, item));

            // Act - 2
            changeProvider.TokenSource.Cancel();
            var collection2 = actionDescriptorCollectionProvider.ActionDescriptors;

            // Assert - 2
            Assert.NotSame(collection1, collection2);
            Assert.Equal(1, collection2.Version);
            Assert.Collection(collection2.Items,
                item => Assert.Same(expected2, item));

            // Act - 3
            changeProvider.TokenSource.Cancel();
            var collection3 = actionDescriptorCollectionProvider.ActionDescriptors;

            // Assert - 3
            Assert.NotSame(collection2, collection3);
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
}
