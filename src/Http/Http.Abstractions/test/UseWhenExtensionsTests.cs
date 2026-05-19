// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.Extensions;

public class UseWhenExtensionsTests
{
    [Fact]
    public void NullArguments_ArgumentNullException()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        Action nullPredicate = () => builder.UseWhen(null!, app => { });
        Action nullConfiguration = () => builder.UseWhen(TruePredicate, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(nullPredicate);
        Assert.Throws<ArgumentNullException>(nullConfiguration);
    }

    [Fact]
    public async Task PredicateTrue_BranchTaken_WillRejoin()
    {
        // Arrange
        var context = CreateContext();
        var parent = CreateBuilder();

        parent.UseWhen(TruePredicate, child =>
        {
            child.UseWhen(TruePredicate, grandchild =>
            {
                grandchild.Use(Increment("grandchild"));
            });

            child.Use(Increment("child"));
        });

        parent.Use(Increment("parent"));

        // Act
        await parent.Build().Invoke(context);

        // Assert
        Assert.Equal(1, Count(context, "parent"));
        Assert.Equal(1, Count(context, "child"));
        Assert.Equal(1, Count(context, "grandchild"));
    }

    [Fact]
    public async Task PredicateTrue_BranchTaken_CanTerminate()
    {
        // Arrange
        var context = CreateContext();
        var parent = CreateBuilder();

        parent.UseWhen(TruePredicate, child =>
        {
            child.UseWhen(TruePredicate, grandchild =>
            {
                grandchild.Use(Increment("grandchild", terminate: true));
            });

            child.Use(Increment("child"));
        });

        parent.Use(Increment("parent"));

        // Act
        await parent.Build().Invoke(context);

        // Assert
        Assert.Equal(0, Count(context, "parent"));
        Assert.Equal(0, Count(context, "child"));
        Assert.Equal(1, Count(context, "grandchild"));
    }

    [Fact]
    public async Task PredicateFalse_PassThrough()
    {
        // Arrange
        var context = CreateContext();
        var parent = CreateBuilder();

        parent.UseWhen(FalsePredicate, child =>
        {
            child.Use(Increment("child"));
        });

        parent.Use(Increment("parent"));

        // Act
        await parent.Build().Invoke(context);

        // Assert
        Assert.Equal(1, Count(context, "parent"));
        Assert.Equal(0, Count(context, "child"));
    }

    private static HttpContext CreateContext()
    {
        return new DefaultHttpContext();
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return new ApplicationBuilder(serviceProvider: null!);
    }

    private static bool TruePredicate(HttpContext context)
    {
        return true;
    }

    private static bool FalsePredicate(HttpContext context)
    {
        return false;
    }

    private static Func<HttpContext, Func<Task>, Task> Increment(string key, bool terminate = false)
    {
        return (context, next) =>
        {
            if (!context.Items.ContainsKey(key))
            {
                context.Items[key] = 1;
            }
            else
            {
                var item = context.Items[key];

                if (item is int)
                {
                    context.Items[key] = 1 + (int)item;
                }
                else
                {
                    context.Items[key] = 1;
                }
            }

            return terminate ? Task.FromResult<object?>(null) : next();
        };
    }

    private static int Count(HttpContext context, string key)
    {
        if (!context.Items.ContainsKey(key))
        {
            return 0;
        }

        var item = context.Items[key];

        if (item is int)
        {
            return (int)item;
        }

        return 0;
    }
}
