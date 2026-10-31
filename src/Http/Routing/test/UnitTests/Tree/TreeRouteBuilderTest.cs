// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Routing.Tree;

public class TreeRouteBuilderTest
{
    [Fact]
    public void TreeRouter_BuildThrows_RoutesWithTheSameNameAndDifferentTemplates()
    {
        // Arrange
        var builder = CreateBuilder();

        var message = "Two or more routes named 'Get_Products' have different templates.";

        builder.MapOutbound(
            Mock.Of<IRouter>(),
            TemplateParser.Parse("api/Products"),
            new RouteValueDictionary(),
            "Get_Products",
            order: 0);

        builder.MapOutbound(
            Mock.Of<IRouter>(),
            TemplateParser.Parse("Products/Index"),
            new RouteValueDictionary(),
            "Get_Products",
            order: 0);

        // Act & Assert
        ExceptionAssert.ThrowsArgument(() =>
        {
            builder.Build();
        }, "linkGenerationEntries", message);
    }

    [Fact]
    public void TreeRouter_BuildDoesNotThrow_RoutesWithTheSameNameAndSameTemplates()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapOutbound(
            Mock.Of<IRouter>(),
            TemplateParser.Parse("api/Products"),
            new RouteValueDictionary(),
            "Get_Products",
            order: 0);

        builder.MapOutbound(
            Mock.Of<IRouter>(),
            TemplateParser.Parse("api/products"),
            new RouteValueDictionary(),
            "Get_Products",
            order: 0);

        // Act & Assert (does not throw)
        builder.Build();
    }

    [Fact]
    public void TreeRouter_BuildDoesNotAddIntermediateMatchingNodes_ForRoutesWithIntermediateParametersWithDefaultValues()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapInbound(
            Mock.Of<IRouter>(),
            TemplateParser.Parse("a/{b=3}/c"),
            "Intermediate",
            order: 0);

        // Act
        var tree = builder.Build();

        // Assert
        Assert.NotNull(tree);
        Assert.NotNull(tree.MatchingTrees);
        var matchingTree = Assert.Single(tree.MatchingTrees);

        var firstSegment = Assert.Single(matchingTree.Root.Literals);
        Assert.Equal("a", firstSegment.Key);
        Assert.NotNull(firstSegment.Value.Parameters);

        var secondSegment = firstSegment.Value.Parameters;
        Assert.Empty(secondSegment.Matches);

        var thirdSegment = Assert.Single(secondSegment.Literals);
        Assert.Equal("c", thirdSegment.Key);
        Assert.Single(thirdSegment.Value.Matches);
    }

    [Fact]
    public void TreeRouter_BuildDoesNotAddIntermediateMatchingNodes_ForRoutesWithMultipleIntermediateParametersWithDefaultOrOptionalValues()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapInbound(
            Mock.Of<IRouter>(),
            TemplateParser.Parse("a/{b=3}/c/{d?}/e/{*f}"),
            "Intermediate",
            order: 0);

        // Act
        var tree = builder.Build();

        // Assert
        Assert.NotNull(tree);
        Assert.NotNull(tree.MatchingTrees);
        var matchingTree = Assert.Single(tree.MatchingTrees);

        var firstSegment = Assert.Single(matchingTree.Root.Literals);
        Assert.Equal("a", firstSegment.Key);
        Assert.NotNull(firstSegment.Value.Parameters);

        var secondSegment = firstSegment.Value.Parameters;
        Assert.Empty(secondSegment.Matches);

        var thirdSegment = Assert.Single(secondSegment.Literals);
        Assert.Equal("c", thirdSegment.Key);
        Assert.Empty(thirdSegment.Value.Matches);

        var fourthSegment = thirdSegment.Value.Parameters;
        Assert.NotNull(fourthSegment);
        Assert.Empty(fourthSegment.Matches);

        var fifthSegment = Assert.Single(fourthSegment.Literals);
        Assert.Equal("e", fifthSegment.Key);
        Assert.Single(fifthSegment.Value.Matches);

        var sixthSegment = fifthSegment.Value.CatchAlls;
        Assert.NotNull(sixthSegment);
        Assert.Single(sixthSegment.Matches);
    }

    [Fact]
    public void TreeRouter_BuildDoesNotAddIntermediateMatchingNodes_ForRoutesWithIntermediateParametersWithOptionalValues()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapInbound(
            Mock.Of<IRouter>(),
            TemplateParser.Parse("a/{b?}/c"),
            "Intermediate",
            order: 0);

        // Act
        var tree = builder.Build();

        // Assert
        Assert.NotNull(tree);
        Assert.NotNull(tree.MatchingTrees);
        var matchingTree = Assert.Single(tree.MatchingTrees);

        var firstSegment = Assert.Single(matchingTree.Root.Literals);
        Assert.Equal("a", firstSegment.Key);
        Assert.NotNull(firstSegment.Value.Parameters);

        var secondSegment = firstSegment.Value.Parameters;
        Assert.Empty(secondSegment.Matches);

        var thirdSegment = Assert.Single(secondSegment.Literals);
        Assert.Equal("c", thirdSegment.Key);
        Assert.Single(thirdSegment.Value.Matches);
    }

    [Fact]
    public void TreeRouter_BuildDoesNotAddIntermediateMatchingNodes_ForRoutesWithIntermediateParametersWithConstrainedDefaultValues()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapInbound(
            Mock.Of<IRouter>(),
            TemplateParser.Parse("a/{b:int=3}/c"),
            "Intermediate",
            order: 0);

        // Act
        var tree = builder.Build();

        // Assert
        Assert.NotNull(tree);
        Assert.NotNull(tree.MatchingTrees);
        var matchingTree = Assert.Single(tree.MatchingTrees);

        var firstSegment = Assert.Single(matchingTree.Root.Literals);
        Assert.Equal("a", firstSegment.Key);
        Assert.NotNull(firstSegment.Value.ConstrainedParameters);

        var secondSegment = firstSegment.Value.ConstrainedParameters;
        Assert.Empty(secondSegment.Matches);

        var thirdSegment = Assert.Single(secondSegment.Literals);
        Assert.Equal("c", thirdSegment.Key);
        Assert.Single(thirdSegment.Value.Matches);
    }

    [Fact]
    public void TreeRouter_BuildDoesNotAddIntermediateMatchingNodes_ForRoutesWithIntermediateParametersWithConstrainedOptionalValues()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapInbound(
            Mock.Of<IRouter>(),
            TemplateParser.Parse("a/{b:int?}/c"),
            "Intermediate",
            order: 0);

        // Act
        var tree = builder.Build();

        // Assert
        Assert.NotNull(tree);
        Assert.NotNull(tree.MatchingTrees);
        var matchingTree = Assert.Single(tree.MatchingTrees);

        var firstSegment = Assert.Single(matchingTree.Root.Literals);
        Assert.Equal("a", firstSegment.Key);
        Assert.NotNull(firstSegment.Value.ConstrainedParameters);

        var secondSegment = firstSegment.Value.ConstrainedParameters;
        Assert.Empty(secondSegment.Matches);

        var thirdSegment = Assert.Single(secondSegment.Literals);
        Assert.Equal("c", thirdSegment.Key);
        Assert.Single(thirdSegment.Value.Matches);
    }

    private static TreeRouteBuilder CreateBuilder()
    {
        var objectPoolProvider = new DefaultObjectPoolProvider();
        var objectPolicy = new UriBuilderContextPooledObjectPolicy();
        var objectPool = objectPoolProvider.Create(objectPolicy);

        var constraintResolver = GetInlineConstraintResolver();
        var builder = new TreeRouteBuilder(
            NullLoggerFactory.Instance,
            objectPool,
            constraintResolver);
        return builder;
    }

    private static IInlineConstraintResolver GetInlineConstraintResolver()
    {
        var services = new ServiceCollection().AddOptions();
        var serviceProvider = services.BuildServiceProvider();
        var accessor = serviceProvider.GetRequiredService<IOptions<RouteOptions>>();
        return new DefaultInlineConstraintResolver(accessor, serviceProvider);
    }
}
