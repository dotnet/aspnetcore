// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ActionConstraints;

public class ActionConstraintCacheTest
{
    [Fact]
    public void GetActionConstraints_CachesAllActionConstraints()
    {
        // Arrange
        var services = CreateServices();
        var cache = CreateCache(new DefaultActionConstraintProvider());

        var action = new ControllerActionDescriptor()
        {
            ActionConstraints = new[]
            {
                    new TestActionConstraint(),
                    new TestActionConstraint()
                },
        };
        var context = new DefaultHttpContext();

        // Act - 1
        var actionConstraints1 = cache.GetActionConstraints(context, action);

        // Assert - 1
        Assert.Collection(
            actionConstraints1,
            a => Assert.Same(action.ActionConstraints[0], a), // Copied by provider
            a => Assert.Same(action.ActionConstraints[1], a)); // Copied by provider

        // Act - 2
        var actionConstraints2 = cache.GetActionConstraints(context, action);

        Assert.Same(actionConstraints1, actionConstraints2);

        Assert.Collection(
            actionConstraints2,
            a => Assert.Same(actionConstraints1[0], a), // Cached
            a => Assert.Same(actionConstraints1[1], a)); // Cached
    }

    [Fact]
    public void GetActionConstraints_CachesActionConstraintFromFactory()
    {
        // Arrange
        var services = CreateServices();
        var cache = CreateCache(new DefaultActionConstraintProvider());

        var action = new ControllerActionDescriptor()
        {
            ActionConstraints = new[]
            {
                    new TestActionConstraintFactory() { IsReusable = true },
                    new TestActionConstraint() as IActionConstraintMetadata
                },
        };
        var context = new DefaultHttpContext();

        // Act - 1
        var actionConstraints1 = cache.GetActionConstraints(context, action);

        // Assert - 1
        Assert.Collection(
            actionConstraints1,
            a => Assert.NotSame(action.ActionConstraints[0], a), // Created by factory
            a => Assert.Same(action.ActionConstraints[1], a)); // Copied by provider

        // Act - 2
        var actionConstraints2 = cache.GetActionConstraints(context, action);

        Assert.Same(actionConstraints1, actionConstraints2);

        Assert.Collection(
            actionConstraints2,
            a => Assert.Same(actionConstraints1[0], a), // Cached
            a => Assert.Same(actionConstraints1[1], a)); // Cached
    }

    [Fact]
    public void GetActionConstraints_DoesNotCacheActionConstraintsWithIsReusableFalse()
    {
        // Arrange
        var services = CreateServices();
        var cache = CreateCache(new DefaultActionConstraintProvider());

        var action = new ControllerActionDescriptor()
        {
            ActionConstraints = new[]
            {
                    new TestActionConstraintFactory() { IsReusable = false },
                    new TestActionConstraint() as IActionConstraintMetadata
                },
        };
        var context = new DefaultHttpContext();

        // Act - 1
        var actionConstraints1 = cache.GetActionConstraints(context, action);

        // Assert - 1
        Assert.Collection(
            actionConstraints1,
            a => Assert.NotSame(action.ActionConstraints[0], a), // Created by factory
            a => Assert.Same(action.ActionConstraints[1], a)); // Copied by provider

        // Act - 2
        var actionConstraints2 = cache.GetActionConstraints(context, action);

        Assert.NotSame(actionConstraints1, actionConstraints2);

        Assert.Collection(
            actionConstraints2,
            a => Assert.NotSame(actionConstraints1[0], a), // Created by factory (again)
            a => Assert.Same(actionConstraints1[1], a)); // Cached
    }

    private class TestActionConstraint : IActionConstraint
    {
        public int Order
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool Accept(ActionConstraintContext context)
        {
            throw new NotImplementedException();
        }
    }

    private class TestActionConstraintFactory : IActionConstraintFactory
    {
        public bool IsReusable { get; set; }

        public IActionConstraint CreateInstance(IServiceProvider serviceProvider)
        {
            return new TestActionConstraint();
        }
    }

    private static IServiceProvider CreateServices()
    {
        return new ServiceCollection().BuildServiceProvider();
    }

    private static ActionConstraintCache CreateCache(params IActionConstraintProvider[] providers)
    {
        var descriptorProvider = new DefaultActionDescriptorCollectionProvider(
            Enumerable.Empty<IActionDescriptorProvider>(),
            Enumerable.Empty<IActionDescriptorChangeProvider>(),
            NullLogger<DefaultActionDescriptorCollectionProvider>.Instance);
        return new ActionConstraintCache(descriptorProvider, providers);
    }
}
