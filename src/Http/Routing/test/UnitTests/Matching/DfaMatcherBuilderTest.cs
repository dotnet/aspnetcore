// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Routing.Matching;

public class DfaMatcherBuilderTest
{
    [Fact]
    public void BuildDfaTree_SingleEndpoint_Empty()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint = CreateEndpoint("/");
        builder.AddEndpoint(endpoint);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Same(endpoint, Assert.Single(root.Matches));
        Assert.Null(root.Parameters);
        Assert.Null(root.Literals);
    }

    [Fact]
    public void BuildDfaTree_SingleEndpoint_Literals()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint = CreateEndpoint("a/b/c");
        builder.AddEndpoint(endpoint);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Null(a.Matches);
        Assert.Null(a.Parameters);

        next = Assert.Single(a.Literals);
        Assert.Equal("b", next.Key);

        var b = next.Value;
        Assert.Null(b.Matches);
        Assert.Null(b.Parameters);

        next = Assert.Single(b.Literals);
        Assert.Equal("c", next.Key);

        var c = next.Value;
        Assert.Same(endpoint, Assert.Single(c.Matches));
        Assert.Null(c.Parameters);
        Assert.Null(c.Literals);
    }

    [Fact]
    public void BuildDfaTree_SingleEndpoint_Parameters()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint = CreateEndpoint("{a}/{b}/{c}");
        builder.AddEndpoint(endpoint);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Literals);

        var a = root.Parameters;
        Assert.Null(a.Matches);
        Assert.Null(a.Literals);

        var b = a.Parameters;
        Assert.Null(b.Matches);
        Assert.Null(b.Literals);

        var c = b.Parameters;
        Assert.Same(endpoint, Assert.Single(c.Matches));
        Assert.Null(c.Parameters);
        Assert.Null(c.Literals);
    }

    [Fact]
    public void BuildDfaTree_SingleEndpoint_CatchAll()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint = CreateEndpoint("{a}/{*b}");
        builder.AddEndpoint(endpoint);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Literals);

        var a = root.Parameters;

        // The catch all can match a path like '/a'
        Assert.Same(endpoint, Assert.Single(a.Matches));
        Assert.Null(a.Literals);
        Assert.Null(a.Parameters);

        // Catch-all nodes include an extra transition that loops to process
        // extra segments.
        var catchAll = a.CatchAll;
        Assert.Same(endpoint, Assert.Single(catchAll.Matches));
        Assert.Null(catchAll.Literals);
        Assert.Same(catchAll, catchAll.Parameters);
        Assert.Same(catchAll, catchAll.CatchAll);
    }

    [Fact]
    public void BuildDfaTree_SingleEndpoint_CatchAllAtRoot()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint = CreateEndpoint("{*a}");
        builder.AddEndpoint(endpoint);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Same(endpoint, Assert.Single(root.Matches));
        Assert.Null(root.Literals);

        // Catch-all nodes include an extra transition that loops to process
        // extra segments.
        var catchAll = root.CatchAll;
        Assert.Same(endpoint, Assert.Single(catchAll.Matches));
        Assert.Null(catchAll.Literals);
        Assert.Same(catchAll, catchAll.Parameters);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_LiteralAndLiteral()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a/b1/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a/b2/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Null(a.Matches);

        Assert.Equal(2, a.Literals.Count);

        var b1 = a.Literals["b1"];
        Assert.Null(b1.Matches);
        Assert.Null(b1.Parameters);

        next = Assert.Single(b1.Literals);
        Assert.Equal("c", next.Key);

        var c1 = next.Value;
        Assert.Same(endpoint1, Assert.Single(c1.Matches));
        Assert.Null(c1.Parameters);
        Assert.Null(c1.Literals);

        var b2 = a.Literals["b2"];
        Assert.Null(b2.Matches);
        Assert.Null(b2.Parameters);

        next = Assert.Single(b2.Literals);
        Assert.Equal("c", next.Key);

        var c2 = next.Value;
        Assert.Same(endpoint2, Assert.Single(c2.Matches));
        Assert.Null(c2.Parameters);
        Assert.Null(c2.Literals);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_LiteralDifferentCase()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a/b1/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("A/b2/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Null(a.Matches);

        Assert.Equal(2, a.Literals.Count);

        var b1 = a.Literals["b1"];
        Assert.Null(b1.Matches);
        Assert.Null(b1.Parameters);

        next = Assert.Single(b1.Literals);
        Assert.Equal("c", next.Key);

        var c1 = next.Value;
        Assert.Same(endpoint1, Assert.Single(c1.Matches));
        Assert.Null(c1.Parameters);
        Assert.Null(c1.Literals);

        var b2 = a.Literals["b2"];
        Assert.Null(b2.Matches);
        Assert.Null(b2.Parameters);

        next = Assert.Single(b2.Literals);
        Assert.Equal("c", next.Key);

        var c2 = next.Value;
        Assert.Same(endpoint2, Assert.Single(c2.Matches));
        Assert.Null(c2.Parameters);
        Assert.Null(c2.Literals);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_LiteralAndParameter()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a/b/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a/{b}/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Null(a.Matches);

        next = Assert.Single(a.Literals);
        Assert.Equal("b", next.Key);

        var b = next.Value;
        Assert.Null(b.Matches);
        Assert.Null(b.Parameters);

        next = Assert.Single(b.Literals);
        Assert.Equal("c", next.Key);

        var c1 = next.Value;
        Assert.Collection(
            c1.Matches,
            e => Assert.Same(endpoint1, e),
            e => Assert.Same(endpoint2, e));
        Assert.Null(c1.Parameters);
        Assert.Null(c1.Literals);

        var b2 = a.Parameters;
        Assert.Null(b2.Matches);
        Assert.Null(b2.Parameters);

        next = Assert.Single(b2.Literals);
        Assert.Equal("c", next.Key);

        var c2 = next.Value;
        Assert.Same(endpoint2, Assert.Single(c2.Matches));
        Assert.Null(c2.Parameters);
        Assert.Null(c2.Literals);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ParameterAndParameter()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a/{b1}/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a/{b2}/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Null(a.Matches);
        Assert.Null(a.Literals);

        var b = a.Parameters;
        Assert.Null(b.Matches);
        Assert.Null(b.Parameters);

        next = Assert.Single(b.Literals);
        Assert.Equal("c", next.Key);

        var c = next.Value;
        Assert.Collection(
            c.Matches,
            e => Assert.Same(endpoint1, e),
            e => Assert.Same(endpoint2, e));
        Assert.Null(c.Parameters);
        Assert.Null(c.Literals);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_LiteralAndCatchAll()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a/b/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a/{*b}");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Same(endpoint2, Assert.Single(a.Matches));

        next = Assert.Single(a.Literals);
        Assert.Equal("b", next.Key);

        var b1 = next.Value;
        Assert.Same(endpoint2, Assert.Single(a.Matches));
        Assert.Null(b1.Parameters);

        next = Assert.Single(b1.Literals);
        Assert.Equal("c", next.Key);

        var c1 = next.Value;
        Assert.Collection(
            c1.Matches,
            e => Assert.Same(endpoint1, e),
            e => Assert.Same(endpoint2, e));
        Assert.Null(c1.Parameters);
        Assert.Null(c1.Literals);

        var catchAll = a.CatchAll;
        Assert.Same(endpoint2, Assert.Single(catchAll.Matches));
        Assert.Same(catchAll, catchAll.Parameters);
        Assert.Same(catchAll, catchAll.CatchAll);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ParameterAndCatchAll()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a/{b}/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a/{*b}");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Same(endpoint2, Assert.Single(a.Matches));
        Assert.Null(a.Literals);

        var b1 = a.Parameters;
        Assert.Same(endpoint2, Assert.Single(a.Matches));
        Assert.Null(b1.Parameters);

        next = Assert.Single(b1.Literals);
        Assert.Equal("c", next.Key);

        var c1 = next.Value;
        Assert.Collection(
            c1.Matches,
            e => Assert.Same(endpoint1, e),
            e => Assert.Same(endpoint2, e));
        Assert.Null(c1.Parameters);
        Assert.Null(c1.Literals);

        var catchAll = a.CatchAll;
        Assert.Same(endpoint2, Assert.Single(catchAll.Matches));
        Assert.Same(catchAll, catchAll.Parameters);
        Assert.Same(catchAll, catchAll.CatchAll);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ConstrainedParameterTrimming_DoesNotMeetConstraint()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("{a:length(2)}/b/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.NotNull(root.Parameters);

        var aNodeKvp = Assert.Single(root.Literals);
        Assert.Equal("a", aNodeKvp.Key);

        var aNodeValue = aNodeKvp.Value;
        var cNodeKvp = Assert.Single(aNodeValue.Literals);
        Assert.Equal("c", cNodeKvp.Key);
        var cNode = cNodeKvp.Value;

        Assert.Same(endpoint1, Assert.Single(cNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        var bNodeKvp = Assert.Single(root.Parameters.Literals);
        Assert.Equal("b", bNodeKvp.Key);
        var bNode = bNodeKvp.Value;
        Assert.Null(bNode.Parameters);
        Assert.Null(bNode.Matches);
        var paramCNodeKvp = Assert.Single(bNode.Literals);

        Assert.Equal("c", paramCNodeKvp.Key);
        var paramCNode = paramCNodeKvp.Value;
        Assert.Same(endpoint2, Assert.Single(paramCNode.Matches));
        Assert.Null(paramCNode.Literals);
        Assert.Null(paramCNode.Parameters);
    }

    [Theory]
    [InlineData("aa/c", "aa", "c")]
    [InlineData("1/c", "1", "c")]
    public void BuildDfaTree_MultipleEndpoint_ConstrainedParameterTrimming_EvaluatesAllConstraints(string candidate, string firstSegment, string secondSegment)
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint(candidate);
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("{a:int:length(2)}/b/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.NotNull(root.Parameters);

        var aNodeKvp = Assert.Single(root.Literals);
        Assert.Equal(firstSegment, aNodeKvp.Key);

        var aNodeValue = aNodeKvp.Value;
        var cNodeKvp = Assert.Single(aNodeValue.Literals);
        Assert.Equal(secondSegment, cNodeKvp.Key);
        var cNode = cNodeKvp.Value;

        Assert.Same(endpoint1, Assert.Single(cNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        var bNodeKvp = Assert.Single(root.Parameters.Literals);
        Assert.Equal("b", bNodeKvp.Key);
        var bNode = bNodeKvp.Value;
        Assert.Null(bNode.Parameters);
        Assert.Null(bNode.Matches);
        var paramCNodeKvp = Assert.Single(bNode.Literals);

        Assert.Equal("c", paramCNodeKvp.Key);
        var paramCNode = paramCNodeKvp.Value;
        Assert.Same(endpoint2, Assert.Single(paramCNode.Matches));
        Assert.Null(paramCNode.Literals);
        Assert.Null(paramCNode.Parameters);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ConstrainedParameterTrimming_MeetsConstraint()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("aa/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("{a:length(2)}/b/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.NotNull(root.Parameters);

        // Branch aa -> c = (aa/c)

        var aNodeKvp = Assert.Single(root.Literals);
        Assert.Equal("aa", aNodeKvp.Key);

        var aNodeValue = aNodeKvp.Value;
        Assert.True(aNodeValue.Literals.TryGetValue("c", out var cNode));

        Assert.Same(endpoint1, Assert.Single(cNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch (aa) -> b -> c = ({a:length(2)}/b/c)

        Assert.True(aNodeValue.Literals.TryGetValue("b", out var bNode));
        Assert.Null(bNode.Parameters);
        Assert.Null(bNode.Matches);
        var paramBCNodeKvp = Assert.Single(bNode.Literals);
        Assert.Equal("c", paramBCNodeKvp.Key);
        var paramBCNode = paramBCNodeKvp.Value;

        Assert.Same(endpoint2, Assert.Single(paramBCNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch {param} -> b -> c = ({a:length(2)}/b/c)

        var bParamNodeKvp = Assert.Single(root.Parameters.Literals);
        Assert.Equal("b", bParamNodeKvp.Key);
        var bParamNode = bParamNodeKvp.Value;
        Assert.Null(bParamNode.Parameters);
        Assert.Null(bParamNode.Matches);
        var paramCNodeKvp = Assert.Single(bParamNode.Literals);

        Assert.Equal("c", paramCNodeKvp.Key);
        var paramCNode = paramCNodeKvp.Value;
        Assert.Same(endpoint2, Assert.Single(paramCNode.Matches));
        Assert.Null(paramCNode.Literals);
        Assert.Null(paramCNode.Parameters);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ConstrainedParameterTrimming_BothCandidates_WhenLitteralPatternMeetsConstraintAndRoutePattern()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("aa/b/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("{a:length(2)}/b/c");
        builder.AddEndpoint(endpoint2);

        var endpoint3 = CreateEndpoint("aa/c");
        builder.AddEndpoint(endpoint3);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.NotNull(root.Parameters);

        // Branch aa -> c = (aa/c)

        var aNodeKvp = Assert.Single(root.Literals);
        Assert.Equal("aa", aNodeKvp.Key);

        var aNodeValue = aNodeKvp.Value;
        Assert.True(aNodeValue.Literals.TryGetValue("c", out var cNode));

        Assert.Same(endpoint3, Assert.Single(cNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch (aa) -> b -> c = (aa/b/c, {a:length(2)}/b/c)

        Assert.True(aNodeValue.Literals.TryGetValue("b", out var bNode));
        Assert.Null(bNode.Parameters);
        Assert.Null(bNode.Matches);
        var paramBCNodeKvp = Assert.Single(bNode.Literals);
        Assert.Equal("c", paramBCNodeKvp.Key);
        var paramBCNode = paramBCNodeKvp.Value;

        Assert.Equal(new[] { endpoint1, endpoint2 }, paramBCNode.Matches.ToArray());
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch {param} -> b -> c = ({a:length(2)}/b/c)

        var bParamNodeKvp = Assert.Single(root.Parameters.Literals);
        Assert.Equal("b", bParamNodeKvp.Key);
        var bParamNode = bParamNodeKvp.Value;
        Assert.Null(bParamNode.Parameters);
        Assert.Null(bParamNode.Matches);
        var paramCNodeKvp = Assert.Single(bParamNode.Literals);

        Assert.Equal("c", paramCNodeKvp.Key);
        var paramCNode = paramCNodeKvp.Value;
        Assert.Same(endpoint2, Assert.Single(paramCNode.Matches));
        Assert.Null(paramCNode.Literals);
        Assert.Null(paramCNode.Parameters);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ComplexParameter_LiteralDoesNotMatchComplexParameter()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a{value}/b/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.NotNull(root.Parameters);

        var aNodeKvp = Assert.Single(root.Literals);
        Assert.Equal("a", aNodeKvp.Key);

        var aNodeValue = aNodeKvp.Value;
        var cNodeKvp = Assert.Single(aNodeValue.Literals);
        Assert.Equal("c", cNodeKvp.Key);
        var cNode = cNodeKvp.Value;

        Assert.Same(endpoint1, Assert.Single(cNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        var bNodeKvp = Assert.Single(root.Parameters.Literals);
        Assert.Equal("b", bNodeKvp.Key);
        var bNode = bNodeKvp.Value;
        Assert.Null(bNode.Parameters);
        Assert.Null(bNode.Matches);
        var paramCNodeKvp = Assert.Single(bNode.Literals);

        Assert.Equal("c", paramCNodeKvp.Key);
        var paramCNode = paramCNodeKvp.Value;
        Assert.Same(endpoint2, Assert.Single(paramCNode.Matches));
        Assert.Null(paramCNode.Literals);
        Assert.Null(paramCNode.Parameters);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ComplexParameter_LiteralMatchesComplexParameter()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("aa/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a{value}/b/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.NotNull(root.Parameters);

        // Branch aa -> c = (aa/c)

        var aNodeKvp = Assert.Single(root.Literals);
        Assert.Equal("aa", aNodeKvp.Key);

        var aNodeValue = aNodeKvp.Value;
        Assert.True(aNodeValue.Literals.TryGetValue("c", out var cNode));

        Assert.Same(endpoint1, Assert.Single(cNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch (aa) -> b -> c = (a{value}/b/c)

        Assert.True(aNodeValue.Literals.TryGetValue("b", out var bNode));
        Assert.Null(bNode.Parameters);
        Assert.Null(bNode.Matches);
        var paramBCNodeKvp = Assert.Single(bNode.Literals);
        Assert.Equal("c", paramBCNodeKvp.Key);
        var paramBCNode = paramBCNodeKvp.Value;

        Assert.Same(endpoint2, Assert.Single(paramBCNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch {param} -> b -> c = (a{value}/b/c)

        var bParamNodeKvp = Assert.Single(root.Parameters.Literals);
        Assert.Equal("b", bParamNodeKvp.Key);
        var bParamNode = bParamNodeKvp.Value;
        Assert.Null(bParamNode.Parameters);
        Assert.Null(bParamNode.Matches);
        var paramCNodeKvp = Assert.Single(bParamNode.Literals);

        Assert.Equal("c", paramCNodeKvp.Key);
        var paramCNode = paramCNodeKvp.Value;
        Assert.Same(endpoint2, Assert.Single(paramCNode.Matches));
        Assert.Null(paramCNode.Literals);
        Assert.Null(paramCNode.Parameters);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ConstrainedComplexParameter_LiteralMatchesComplexParameterButNotConstraint()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("aa/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a{value:int}/b/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.NotNull(root.Parameters);

        var aNodeKvp = Assert.Single(root.Literals);
        Assert.Equal("aa", aNodeKvp.Key);

        var aNodeValue = aNodeKvp.Value;
        var cNodeKvp = Assert.Single(aNodeValue.Literals);
        Assert.Equal("c", cNodeKvp.Key);
        var cNode = cNodeKvp.Value;

        Assert.Same(endpoint1, Assert.Single(cNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        var bNodeKvp = Assert.Single(root.Parameters.Literals);
        Assert.Equal("b", bNodeKvp.Key);
        var bNode = bNodeKvp.Value;
        Assert.Null(bNode.Parameters);
        Assert.Null(bNode.Matches);
        var paramCNodeKvp = Assert.Single(bNode.Literals);

        Assert.Equal("c", paramCNodeKvp.Key);
        var paramCNode = paramCNodeKvp.Value;
        Assert.Same(endpoint2, Assert.Single(paramCNode.Matches));
        Assert.Null(paramCNode.Literals);
        Assert.Null(paramCNode.Parameters);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ComplexParameter_LiteralMatchesComplexParameterAndPartConstraint()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a1/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a{value:int}/b/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.NotNull(root.Parameters);

        // Branch aa -> c = (a1/c)

        var aNodeKvp = Assert.Single(root.Literals);
        Assert.Equal("a1", aNodeKvp.Key);

        var aNodeValue = aNodeKvp.Value;
        Assert.True(aNodeValue.Literals.TryGetValue("c", out var cNode));

        Assert.Same(endpoint1, Assert.Single(cNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch (a1) -> b -> c = (a{value:int}/b/c)

        Assert.True(aNodeValue.Literals.TryGetValue("b", out var bNode));
        Assert.Null(bNode.Parameters);
        Assert.Null(bNode.Matches);
        var paramBCNodeKvp = Assert.Single(bNode.Literals);
        Assert.Equal("c", paramBCNodeKvp.Key);
        var paramBCNode = paramBCNodeKvp.Value;

        Assert.Same(endpoint2, Assert.Single(paramBCNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch {param} -> b -> c = (a{value:int}/b/c)

        var bParamNodeKvp = Assert.Single(root.Parameters.Literals);
        Assert.Equal("b", bParamNodeKvp.Key);
        var bParamNode = bParamNodeKvp.Value;
        Assert.Null(bParamNode.Parameters);
        Assert.Null(bParamNode.Matches);
        var paramCNodeKvp = Assert.Single(bParamNode.Literals);

        Assert.Equal("c", paramCNodeKvp.Key);
        var paramCNode = paramCNodeKvp.Value;
        Assert.Same(endpoint2, Assert.Single(paramCNode.Matches));
        Assert.Null(paramCNode.Literals);
        Assert.Null(paramCNode.Parameters);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ComplexParameter_EvaluatesAllPartsAndConstraints()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a-11-b-true/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a-{value:int:length(2)}-b-{other:bool}/b/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.NotNull(root.Parameters);

        // Branch a11-b-true -> c = (a11-b-true/c)

        var aNodeKvp = Assert.Single(root.Literals);
        Assert.Equal("a-11-b-true", aNodeKvp.Key);

        var aNodeValue = aNodeKvp.Value;
        Assert.True(aNodeValue.Literals.TryGetValue("c", out var cNode));

        Assert.Same(endpoint1, Assert.Single(cNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch (a-11-b-true) -> b -> c = (a-{value:int:length(2)}-b-{other:bool}/b/c)

        Assert.True(aNodeValue.Literals.TryGetValue("b", out var bNode));
        Assert.Null(bNode.Parameters);
        Assert.Null(bNode.Matches);
        var paramBCNodeKvp = Assert.Single(bNode.Literals);
        Assert.Equal("c", paramBCNodeKvp.Key);
        var paramBCNode = paramBCNodeKvp.Value;

        Assert.Same(endpoint2, Assert.Single(paramBCNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch {param} -> b -> c = (a-{value:int:length(2)}-b-{other:bool}/b/c)

        var bParamNodeKvp = Assert.Single(root.Parameters.Literals);
        Assert.Equal("b", bParamNodeKvp.Key);
        var bParamNode = bParamNodeKvp.Value;
        Assert.Null(bParamNode.Parameters);
        Assert.Null(bParamNode.Matches);
        var paramCNodeKvp = Assert.Single(bParamNode.Literals);

        Assert.Equal("c", paramCNodeKvp.Key);
        var paramCNode = paramCNodeKvp.Value;
        Assert.Same(endpoint2, Assert.Single(paramCNode.Matches));
        Assert.Null(paramCNode.Literals);
        Assert.Null(paramCNode.Parameters);
    }

    [Theory]
    [InlineData("a-11-b-true/c", "a-11-b-true", "c")]
    [InlineData("a-ddd-b-true/c", "a-ddd-b-true", "c")]
    [InlineData("a-111-b-0/c", "a-111-b-0", "c")]
    public void BuildDfaTree_MultipleEndpoint_ComplexParameter_Trims_When_OneConstraintFails(string candidate, string firstSegment, string secondSegment)
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint(candidate);
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a-{value:int:length(3)}-b-{other:bool}/b/c");
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.NotNull(root.Parameters);

        // Branch a-11-b-true -> c = (a11-b-true/c)

        var aNodeKvp = Assert.Single(root.Literals);
        Assert.Equal(firstSegment, aNodeKvp.Key);

        var aNodeValue = aNodeKvp.Value;
        var cNodeKvp = aNodeValue.Literals.Single();
        Assert.Equal(secondSegment, cNodeKvp.Key);
        var cNode = cNodeKvp.Value;

        Assert.Same(endpoint1, Assert.Single(cNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch {param} -> b -> c = (a-{value:int:length(2)}-b-{other:bool}/b/c)

        var bParamNodeKvp = Assert.Single(root.Parameters.Literals);
        Assert.Equal("b", bParamNodeKvp.Key);
        var bParamNode = bParamNodeKvp.Value;
        Assert.Null(bParamNode.Parameters);
        Assert.Null(bParamNode.Matches);
        var paramCNodeKvp = Assert.Single(bParamNode.Literals);

        Assert.Equal("c", paramCNodeKvp.Key);
        var paramCNode = paramCNodeKvp.Value;
        Assert.Same(endpoint2, Assert.Single(paramCNode.Matches));
        Assert.Null(paramCNode.Literals);
        Assert.Null(paramCNode.Parameters);
    }

    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ComplexParameter_BothCandidates_WhenLitteralPatternMatchesComplexParameterAndRoutePattern()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("aa/b/c");
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a{value}/b/c");
        builder.AddEndpoint(endpoint2);

        var endpoint3 = CreateEndpoint("aa/c");
        builder.AddEndpoint(endpoint3);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.NotNull(root.Parameters);

        // Branch aa -> c = (aa/c)

        var aNodeKvp = Assert.Single(root.Literals);
        Assert.Equal("aa", aNodeKvp.Key);

        var aNodeValue = aNodeKvp.Value;
        Assert.True(aNodeValue.Literals.TryGetValue("c", out var cNode));

        Assert.Same(endpoint3, Assert.Single(cNode.Matches));
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch (aa) -> b -> c = (aa/b/c, a{value}/b/c)

        Assert.True(aNodeValue.Literals.TryGetValue("b", out var bNode));
        Assert.Null(bNode.Parameters);
        Assert.Null(bNode.Matches);
        var paramBCNodeKvp = Assert.Single(bNode.Literals);
        Assert.Equal("c", paramBCNodeKvp.Key);
        var paramBCNode = paramBCNodeKvp.Value;

        Assert.Equal(new[] { endpoint1, endpoint2 }, paramBCNode.Matches.ToArray());
        Assert.Null(cNode.Literals);
        Assert.Null(cNode.Parameters);

        // Branch {param} -> b -> c = (a{value}/b/c)

        var bParamNodeKvp = Assert.Single(root.Parameters.Literals);
        Assert.Equal("b", bParamNodeKvp.Key);
        var bParamNode = bParamNodeKvp.Value;
        Assert.Null(bParamNode.Parameters);
        Assert.Null(bParamNode.Matches);
        var paramCNodeKvp = Assert.Single(bParamNode.Literals);

        Assert.Equal("c", paramCNodeKvp.Key);
        var paramCNode = paramCNodeKvp.Value;
        Assert.Same(endpoint2, Assert.Single(paramCNode.Matches));
        Assert.Null(paramCNode.Literals);
        Assert.Null(paramCNode.Parameters);
    }

    // Regression test for excessive memory usage https://github.com/dotnet/aspnetcore/issues/23850
    [Fact]
    public void BuildDfaTree_CanHandle_LargeAmountOfRoutes_WithConstraints()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoints = new[]{
                CreateEndpoint("test1/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test1/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test1/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test1/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test1/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test1/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test1/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test1/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test1/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test2/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test2/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test2/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test2/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test2/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test2/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test2/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test2/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test2/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test3/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test3/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test3/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test3/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test3/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test3/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test3/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test3/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test3/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test4/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test4/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test4/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test4/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test4/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test4/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test4/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test4/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test4/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test5/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test5/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test5/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test5/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test5/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test5/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test5/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test5/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test5/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test6/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test6/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test6/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test6/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test6/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test6/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test6/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test6/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test6/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test7/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test7/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test7/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test7/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test7/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test7/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test7/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test7/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test7/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test8/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test8/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test8/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test8/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test8/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test8/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test8/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test8/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test8/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test9/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test9/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test9/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test9/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test9/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test9/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test9/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test9/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test9/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test10/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test10/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test10/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test10/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test10/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test10/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test10/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test10/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test10/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test11/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test11/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test11/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test11/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test11/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test11/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test11/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test11/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test11/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test12/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test12/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test12/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test12/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test12/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test12/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test12/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test12/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test12/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test13/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test13/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test13/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test13/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test13/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test13/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test13/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test13/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test13/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test14/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test14/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test14/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test14/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test14/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test14/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test14/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test14/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test14/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test15/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test15/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test15/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test15/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test15/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test15/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test15/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test15/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test15/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test16/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test16/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test16/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test16/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test16/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test16/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test16/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test16/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test16/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test17/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test17/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test17/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test17/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test17/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test17/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test17/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test17/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test17/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test18/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test18/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test18/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test18/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test18/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test18/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test18/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test18/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test18/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test19/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test19/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test19/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test19/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test19/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test19/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test19/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test19/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test19/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test20/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test20/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test20/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test20/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test20/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test20/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test20/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test20/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test20/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test21/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test21/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test21/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test21/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test21/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test21/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test21/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test21/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test21/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test22/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test22/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test22/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test22/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test22/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test22/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test22/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test22/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test22/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test23/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test23/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test23/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test23/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test23/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test23/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test23/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test23/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test23/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test24/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test24/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test24/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test24/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test24/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test24/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test24/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test24/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test24/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test25/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test25/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test25/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test25/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test25/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test25/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test25/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test25/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test25/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test26/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test26/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test26/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test26/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test26/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test26/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test26/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test26/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test26/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test27/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test27/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test27/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test27/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test27/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test27/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test27/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test27/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test27/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test28/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test28/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test28/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test28/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test28/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test28/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test28/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test28/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test28/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test29/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test29/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test29/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test29/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test29/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test29/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test29/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test29/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test29/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test30/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test30/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test30/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test30/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test30/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test30/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test30/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test30/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test30/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test31/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test31/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test31/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test31/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test31/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test31/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test31/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test31/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test31/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test32/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test32/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test32/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test32/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test32/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test32/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test32/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test32/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test32/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test33/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test33/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test33/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test33/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test33/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test33/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test33/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test33/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test33/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test34/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test34/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test34/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test34/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test34/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test34/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test34/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test34/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test34/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test35/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test35/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test35/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test35/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test35/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test35/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test35/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test35/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test35/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test36/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test36/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test36/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test36/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test36/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test36/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test36/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test36/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test36/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test37/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test37/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test37/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test37/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test37/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test37/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test37/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test37/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test37/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test38/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test38/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test38/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test38/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test38/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test38/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test38/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test38/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test38/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test39/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test39/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test39/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test39/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test39/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test39/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test39/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test39/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test39/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test40/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test40/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test40/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test40/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test40/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test40/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test40/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test40/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test40/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test41/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test41/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test41/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test41/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test41/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test41/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test41/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test41/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test41/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test42/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test42/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test42/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test42/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test42/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test42/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test42/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test42/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test42/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test43/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test43/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test43/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test43/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test43/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test43/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test43/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test43/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test43/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test44/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test44/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test44/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test44/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test44/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test44/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test44/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test44/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test44/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test45/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test45/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test45/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test45/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test45/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test45/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test45/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test45/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test45/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test46/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test46/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test46/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test46/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test46/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test46/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test46/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test46/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test46/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test47/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test47/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test47/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test47/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test47/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test47/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test47/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test47/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test47/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test48/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test48/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test48/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test48/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test48/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test48/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test48/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test48/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test48/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test49/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test49/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test49/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test49/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test49/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test49/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test49/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test49/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test49/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test50/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test50/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test50/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test50/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test50/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test50/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test50/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test50/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test50/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test51/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test51/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test51/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test51/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test51/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test51/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test51/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test51/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test51/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test52/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test52/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test52/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test52/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test52/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test52/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test52/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test52/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test52/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test53/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test53/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test53/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test53/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test53/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test53/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test53/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test53/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test53/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test54/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test54/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test54/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test54/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test54/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test54/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test54/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test54/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test54/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test55/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test55/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test55/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test55/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test55/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test55/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test55/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test55/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test55/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test56/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test56/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test56/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test56/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test56/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test56/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test56/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test56/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test56/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test57/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test57/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test57/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test57/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test57/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test57/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test57/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test57/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test57/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test58/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test58/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test58/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test58/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test58/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test58/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test58/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test58/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test58/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test59/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test59/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test59/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test59/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test59/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test59/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test59/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test59/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test59/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test60/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test60/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test60/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test60/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test60/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test60/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test60/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test60/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test60/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test61/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test61/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test61/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test61/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test61/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test61/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test61/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test61/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test61/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test62/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test62/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test62/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test62/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test62/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test62/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test62/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test62/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test62/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test63/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{language:length(2)}/test63/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test63/method-1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("test63/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test63/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test63/method-2", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("test63/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{language:length(2)}/test63/method-3", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("{version:int}/{language:length(2)}/test63/method-3", new HttpMethodMetadata(new[] { "POST" }))
            };

        foreach (var endpoint in endpoints)
        {
            builder.AddEndpoint(endpoint);
        }

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.NotNull(root);
        var count = 0;
        root.Visit(node => count++);

        // Without filtering it would have resulted in millions of nodes, several GB of memory and minutes
        Assert.Equal(759, count);
    }

    // Regression test for excessive memory usage https://github.com/dotnet/aspnetcore/issues/33735
    [Fact]
    public void BuildDfaTree_Regression_33735()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoints = new[]
        {
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/workers/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/workers/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/workers/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/workers/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/workers/{id:regex((?!(discover)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/workers/{id:regex((?!(discover)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/workerpools", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/workerpools", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/workerpools/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/workerpools/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/workerpools/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/workerpools/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/workerpools/{id:regex((?!(all|summary|supportedtypes|dynamicworkertypes)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/workerpools/{id:regex((?!(all|summary|supportedtypes|dynamicworkertypes)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/workerpools", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/workerpools", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/variables/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/variables/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/variables/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/variables/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/users/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/users/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/users/{id:regex((?!(all|authentication|external-search|identity-metadata|me)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/users", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/users/invitations/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/users/invitations/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/userroles/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/userroles/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/userroles/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/userroles", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/userroles/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/users/{id}/permissions/export", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/users/{id}/permissions/export", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/tenants", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/{id:regex(^Tenants-\\d\u002B$)}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/tenants/{id:regex(^Tenants-\\d\u002B$)}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/tenants/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/{id:regex(^Tenants-\\d\u002B$)}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/tenants/{id:regex(^Tenants-\\d\u002B$)}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/{id:regex(^Tenants-\\d\u002B$)}/logo", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/tenants/{id:regex(^Tenants-\\d\u002B$)}/logo", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/variables-missing", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/tenants/variables-missing", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/tenants", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/status", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/tenants/status", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/{id:regex(^Tenants-\\d\u002B$)}/variables", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/tenants/{id:regex(^Tenants-\\d\u002B$)}/variables", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/tenants/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/{id:regex(^Tenants-\\d\u002B$)}/variables", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/tenants/{id:regex(^Tenants-\\d\u002B$)}/variables", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/{id:regex(^Tenants-\\d\u002B$)}/variables", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/tenants/{id:regex(^Tenants-\\d\u002B$)}/variables", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/{id:regex(^Tenants-\\d\u002B$)}/logo", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/tenants/{id:regex(^Tenants-\\d\u002B$)}/logo", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/{id:regex(^Tenants-\\d\u002B$)}/logo", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/tenants/{id:regex(^Tenants-\\d\u002B$)}/logo", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tenants/tag-test", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/tenants/tag-test", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/teams", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/teams", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/teams/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/teams/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/teams/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/teams/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/teams/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/teams/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/teams", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/teams", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/teams/{id}/scopeduserroles", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/teams/{id}/scopeduserroles", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/teams/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/teams/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/teammembership", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/teammembership", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/teammembership/previewteam", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/teammembership/previewteam", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tagsets/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/tagsets/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tagsets/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/tagsets/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tagsets/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/tagsets/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/tagsets", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/tagsets", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/subscriptions/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/subscriptions/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/subscriptions/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/subscriptions/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/subscriptions/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/subscriptions/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/subscriptions", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/subscriptions", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/steps/{stepId}/{version}/logo", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/steps/deploymenttargets/{stepId}/{version}/logo", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/steps/{stepId}/{version}/schema", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/steps/deploymenttargets/{stepId}/{version}/schema", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/steps/{stepId}/latest-version", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/steps/{stepId}/{version}.js", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/steps/deploymenttargets/{stepId}/{version}.js", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/steps/deploymenttargets", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/spaces/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/spaces/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/spaces", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/spaces/{id:regex(^Spaces-\\d\u002B$)}/search", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/spaces/{id:regex(^Spaces-\\d\u002B$)}/search", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/scopeduserroles/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/scopeduserroles/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/scopeduserroles/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/scopeduserroles/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/runbooks", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/projects/{projectId}/runbooks", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/runbooks/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projects/{projectId}/runbooks/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbooks/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbooks/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbooks/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbooks/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks/{id}/environments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbooks/{id}/environments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks/{id}/environments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbooks/{id}/environments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks/{id}/environments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks/{id}/environments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks/{id}/runbookRuns/preview/{environment}/{tenant}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbooks/{id}/runbookRuns/preview/{environment}/{tenant}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks/{id}/runbookRuns/preview/{environment}/{tenant}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks/{id}/runbookRuns/preview/{environment}/{tenant}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks/{id}/runbookRuns/preview/{environment}/{tenant}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbooks/{id}/runbookRuns/preview/{environment}/{tenant}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks/{id}/runbookRuns/preview/{environment}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbooks/{id}/runbookRuns/preview/{environment}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks/{id}/runbookRuns/preview/{environment}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks/{id}/runbookRuns/preview/{environment}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks/{id}/runbookRuns/preview/{environment}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbooks/{id}/runbookRuns/preview/{environment}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbooks", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbooks", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbooks/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbooks/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks/{id}/runbookRunTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbooks/{id}/runbookRunTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks/{id}/runbookRunTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks/{id}/runbookRunTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks/{id}/runbookRunTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbooks/{id}/runbookRunTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/runbooks/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId}/runbooks/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks/{id}/run", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/projects/{projectId}/runbooks/{id}/run", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks/{id}/run", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks/{id}/run", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks/{id}/run", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/runbooks/{id}/run", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooksnapshots/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/runbooksnapshots/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooksnapshots/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projects/{projectId}/runbooksnapshots/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooksnapshots/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooksnapshots/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbookSnapshots/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbookSnapshots/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbookSnapshots/{idOrName}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbookSnapshots/{idOrName}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbookSnapshots/{idOrName}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbookSnapshots/{idOrName}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbooks/{id}/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbooks/{id}/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbooks/{id}/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbooks/{id}/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbooks/{id}/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbooks/{id}/runbookSnapshots", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbookRuns", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/runbookRuns", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbookRuns", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/projects/{projectId}/runbookRuns", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbookRuns", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbookRuns", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbookruns/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/runbookruns/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbookruns/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projects/{projectId}/runbookruns/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbookruns/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbookruns/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbookRuns/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbookRuns/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbookRuns/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbookRuns/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbookRuns/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbookRuns/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbookProcesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbookProcesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbookProcesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbookProcesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbookProcesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbookProcesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbookProcesses/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbookProcesses/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbookProcesses/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbookProcesses/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbookProcesses/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbookProcesses/{id}/runbookSnapshotTemplate", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbookSnapshots/{id}/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/runbookSnapshots/{id}/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbookSnapshots/{id}/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/runbookSnapshots/{id}/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbookSnapshots/{id}/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbookSnapshots/{id}/runbookRuns", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId}/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/runbookProcesses/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/reporting/deployments/xml", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/reporting/deployments/xml", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/releases/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/releases/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/releases/{id}/deployments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/releases/{id}/deployments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/releases", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/releases", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/channels/{id}/releases", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/channels/{id}/releases", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/channels/{id}/releases", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/channels/{id}/releases", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/channels/{id}/releases", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/channels/{id}/releases", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/releases", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/releases", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/proxies/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/proxies/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/proxies/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/proxies/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/proxies", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/proxies", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/proxies/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/proxies/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projecttriggers/{triggerId}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projecttriggers/{triggerId}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/triggers/{triggerId}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projects/{projectId}/triggers/{triggerId}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/triggers/{triggerId}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/triggers/{triggerId}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projecttriggers/{triggerId}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projecttriggers/{triggerId}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/triggers/{triggerId}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/triggers/{triggerId}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/triggers/{triggerId}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/triggers/{triggerId}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projecttriggers", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projecttriggers", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/triggers", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/triggers", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/triggers", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/triggers", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projects/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/experimental/summaries", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/experimental/summaries", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/vcs/modified", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/projects/{projectId}/vcs/modified", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projectgroups/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projectgroups/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projectgroups/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projectgroups/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projectgroups/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projectgroups/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projectgroups/{id}/projects", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projectgroups/{id}/projects", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projectgroups", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projectgroups", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/octopusservernodes/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/octopusservernodes/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/octopusservernodes/{id:regex((?!(ping|summary)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/octopusservernodes", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/machines/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/machines/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/machines/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/machines/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/machines/{id:regex((?!(discover)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/machines/{id:regex((?!(discover)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/machinepolicies/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/machinepolicies/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/machinepolicies/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/machinepolicies/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/machinepolicies", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/machinepolicies", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/machinepolicies/{id:regex((?!(template)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/machinepolicies/{id:regex((?!(template)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/machinepolicies/{id}/machines", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/machinepolicies/{id}/machines", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/machinepolicies/template", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/machinepolicies/template", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/machinepolicies/{id}/workers", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/machinepolicies/{id}/workers", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/lifecycles/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/lifecycles/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/lifecycles/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/lifecycles/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/lifecycles/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/lifecycles/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/lifecycles", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/lifecycles", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/libraryvariablesets/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/libraryvariablesets/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/libraryvariablesets/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/libraryvariablesets/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/libraryvariablesets/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/libraryvariablesets/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/interruptions/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/interruptions/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/feeds/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/feeds/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/environments/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/environments/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/environments/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/environments/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/environments/{id:regex((?!(summary)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/environments/{id:regex((?!(summary)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/environments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/environments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/deployments", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/deployments", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/deployments/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/deployments/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/deployments/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/deployments/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/deployments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/deployments", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentsettings/ocl/raw", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentsettings/ocl/raw", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/deploymentsettings/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/deploymentsettings/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentsettings", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentsettings", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentsettings", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentsettings", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentsettings/ocl/raw", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentsettings/ocl/raw", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/deploymentsettings/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/deploymentsettings/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentsettings", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentsettings", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentsettings", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentsettings", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/deploymentprocesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/deploymentprocesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentprocesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentprocesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses/ocl/raw", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses/ocl/raw", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/deploymentprocesses/{deploymentProcessId}/template", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/deploymentprocesses/{deploymentProcessId}/template", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses/template", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses/template", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentprocesses/template", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentprocesses/template", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/deploymentprocesses/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/deploymentprocesses/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses/ocl/raw", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses/ocl/raw", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/deploymentprocesses/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/deploymentprocesses/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentprocesses", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentprocesses", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentprocess/validate", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/deploymentprocess/validate", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses/validate", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/deploymentprocesses/validate", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/configuration/retention-default", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/configuration/retention-default", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/configuration/certificates/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/communityactiontemplates/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/communityactiontemplates", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/channels", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/channels", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/channels", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/projects/{projectId}/channels", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/channels", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/channels", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/channels/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/channels/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/channels/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/channels/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/channels/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/channels/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/channels/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/channels/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/channels/{id:regex((?!(rule-test)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/channels/{id:regex((?!(rule-test)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/channels/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/channels/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/channels/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/channels/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/channels", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/channels", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/channels", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/channels", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/channels", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/projects/{projectId:regex(^Projects-\\d\u002B$)}/{gitRef}/channels", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/channels/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId}/channels/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/projects/{projectId}/{gitRef}/channels/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/projects/{projectId}/{gitRef}/channels/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/channels/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/channels/{id}", new HttpMethodMetadata(new[] { "PUT" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/channels/rule-test", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/channels/rule-test", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/channels/rule-test", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/channels/rule-test", new HttpMethodMetadata(new[] { "POST" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/certificates/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/certificates/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/certificates/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/certificates/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/build-information/bulk", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/build-information/bulk", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/build-information/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/build-information/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/artifacts/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/artifacts/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/artifacts/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/artifacts/{id}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/actiontemplates/{id:regex((?!(search|categories)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/actiontemplates/{id:regex((?!(search|categories)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/actiontemplates", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/actiontemplates", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/actiontemplates/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/actiontemplates/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/accounts/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/accounts/{id}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/accounts/{id}/pk", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/accounts/{id}/pk", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/accounts/{id:regex((?!(azureenvironments)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/accounts/{id:regex((?!(azureenvironments)))}", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/{baseSpaceId:regex(Spaces-\\d\u002B)}/accounts/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/accounts/all", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("api/diagnostics/throw", new HttpMethodMetadata(new[] { "GET" })),
            };

        foreach (var endpoint in endpoints)
        {
            builder.AddEndpoint(endpoint);
        }

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.NotNull(root);
        var count = 0;
        root.Visit(node => count++);

        // Without filtering it would have resulted in several order of magnitudes more nodes and much more memory
        Assert.Equal(2964, count);
    }

    // Another regression test based on OData models
    [Fact]
    public void BuildDfaTree_CanHandle_LargeAmountOfRoutes_WithComplexParameters()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoints = new[]{
                CreateEndpoint("Student", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student1", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student1/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student1", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student1({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student1({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student1/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student1({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student1/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student2", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student2", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student2/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student2", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student2({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student2({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student2/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student2({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student2/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student3", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student3", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student3/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student3", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student3({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student3({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student3/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student3({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student3/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student4", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student4", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student4/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student4", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student4({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student4({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student4/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student4({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student4/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student5", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student5", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student5/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student5", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student5({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student5({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student5/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student5({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student5/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student6", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student6", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student6/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student6", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student6({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student6({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student6/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student6({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student6/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student7", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student7", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student7/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student7", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student7({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student7({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student7/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student7({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student7/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student8", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student8", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student8/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student8", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student8({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student8({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student8/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student8({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student8/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student9", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student9", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student9/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student9", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student9({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student9({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student9/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student9({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student9/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student10", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student10", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student10/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student10", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student10({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student10({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student10/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student10({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student10/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student11", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student11", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student11/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student11", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student11({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student11({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student11/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student11({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student11/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student12", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student12", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student12/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student12", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student12({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student12({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student12/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student12({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student12/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student13", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student13", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student13/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student13", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student13({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student13({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student13/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student13({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student13/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student14", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student14", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student14/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student14", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student14({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student14({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student14/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student14({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student14/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student15", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student15", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student15/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student15", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student15({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student15({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student15/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student15({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student15/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student16", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student16", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student16/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student16", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student16({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student16({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student16/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student16({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student16/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student17", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student17", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student17/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student17", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student17({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student17({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student17/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student17({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student17/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student18", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student18", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student18/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student18", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student18({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student18({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student18/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student18({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student18/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student19", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student19", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student19/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student19", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student19({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student19({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student19/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student19({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student19/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student20", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student20", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student20/$count", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}/Student20", new HttpMethodMetadata(new[] { "PATCH" })),
                CreateEndpoint("Student20({propName}={propValue})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student20({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("Student20/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student20({key})", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/Student20/{key}", new HttpMethodMetadata(new[] { "DELETE" })),
                CreateEndpoint("{contextToken}/$metadata", new HttpMethodMetadata(new[] { "GET" })),
                CreateEndpoint("{contextToken}", new HttpMethodMetadata(new[] { "GET" })),
};

        foreach (var endpoint in endpoints)
        {
            builder.AddEndpoint(endpoint);
        }

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.NotNull(root);
        var count = 0;
        root.Visit(node => count++);

        // Without filtering it would have resulted in several order of magnitudes more nodes and much more memory
        Assert.Equal(1453, count);
    }

    // Regression test for https://github.com/dotnet/aspnetcore/issues/16579
    //
    // This case behaves the same for all combinations.
    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ParameterAndCatchAll_OnSameNode_Order1()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a/{b}", order: 0);
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a/{*b}", order: 1);
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Same(endpoint2, Assert.Single(a.Matches));
        Assert.Null(a.Literals);

        var b = a.Parameters;
        Assert.Collection(
            b.Matches,
            e => Assert.Same(endpoint1, e),
            e => Assert.Same(endpoint2, e));
        Assert.Null(b.Literals);
        Assert.Null(b.Parameters);
        Assert.NotNull(b.CatchAll);

        var catchAll = b.CatchAll;
        Assert.Same(endpoint2, Assert.Single(catchAll.Matches));
        Assert.Null(catchAll.Literals);
        Assert.Same(catchAll, catchAll.Parameters);
        Assert.Same(catchAll, catchAll.CatchAll);
    }

    // Regression test for https://github.com/dotnet/aspnetcore/issues/16579
    [Fact]
    public void BuildDfaTree_MultipleEndpoint_ParameterAndCatchAll_OnSameNode_Order2()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a/{*b}", order: 0);
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a/{b}", order: 1);
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Same(endpoint1, Assert.Single(a.Matches));
        Assert.Null(a.Literals);

        var b = a.Parameters;
        Assert.Collection(
            b.Matches,
            e => Assert.Same(endpoint1, e),
            e => Assert.Same(endpoint2, e));
        Assert.Null(b.Literals);
        Assert.Null(b.Parameters);
        Assert.NotNull(b.CatchAll);

        var catchAll = b.CatchAll;
        Assert.Same(endpoint1, Assert.Single(catchAll.Matches));
        Assert.Null(catchAll.Literals);
        Assert.Same(catchAll, catchAll.Parameters);
        Assert.Same(catchAll, catchAll.CatchAll);
    }

    // Regression test for https://github.com/dotnet/aspnetcore/issues/18677
    [Fact]
    public void BuildDfaTree_MultipleEndpoint_CatchAllWithHigherPrecedenceThanParameter_Order1()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("{a}/{b}", order: 0);
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("a/{*b}", order: 1);
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a1 = next.Value;
        Assert.Same(endpoint2, Assert.Single(a1.Matches));
        Assert.Null(a1.Literals);

        var b1 = a1.Parameters;
        Assert.Collection(
            b1.Matches,
            e => Assert.Same(endpoint1, e),
            e => Assert.Same(endpoint2, e));
        Assert.Null(b1.Literals);
        Assert.Null(b1.Parameters);
        Assert.NotNull(b1.CatchAll);

        var catchAll1 = b1.CatchAll;
        Assert.Same(endpoint2, Assert.Single(catchAll1.Matches));
        Assert.Null(catchAll1.Literals);
        Assert.Same(catchAll1, catchAll1.Parameters);
        Assert.Same(catchAll1, catchAll1.CatchAll);

        var a2 = root.Parameters;
        Assert.Null(a2.Matches);
        Assert.Null(a2.Literals);

        var b2 = a2.Parameters;
        Assert.Collection(
            b2.Matches,
            e => Assert.Same(endpoint1, e));
        Assert.Null(b2.Literals);
        Assert.Null(b2.Parameters);
        Assert.Null(b2.CatchAll);
    }

    // Regression test for https://github.com/dotnet/aspnetcore/issues/18677
    [Fact]
    public void BuildDfaTree_MultipleEndpoint_CatchAllWithHigherPrecedenceThanParameter_Order2()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateEndpoint("a/{*b}", order: 0);
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("{a}/{b}", order: 1);
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a1 = next.Value;
        Assert.Same(endpoint1, Assert.Single(a1.Matches));
        Assert.Null(a1.Literals);

        var b1 = a1.Parameters;
        Assert.Collection(
            b1.Matches,
            e => Assert.Same(endpoint1, e),
            e => Assert.Same(endpoint2, e));
        Assert.Null(b1.Literals);
        Assert.Null(b1.Parameters);
        Assert.NotNull(b1.CatchAll);

        var catchAll1 = b1.CatchAll;
        Assert.Same(endpoint1, Assert.Single(catchAll1.Matches));
        Assert.Null(catchAll1.Literals);
        Assert.Same(catchAll1, catchAll1.Parameters);
        Assert.Same(catchAll1, catchAll1.CatchAll);

        var a2 = root.Parameters;
        Assert.Null(a2.Matches);
        Assert.Null(a2.Literals);

        var b2 = a2.Parameters;
        Assert.Collection(
            b2.Matches,
            e => Assert.Same(endpoint2, e));
        Assert.Null(b2.Literals);
        Assert.Null(b2.Parameters);
        Assert.Null(b2.CatchAll);
    }

    private void BuildDfaTree_MultipleEndpoint_CatchAllWithHigherPrecedenceThanParameter_Order2_Legacy30Behavior_Core(DfaMatcherBuilder builder)
    {
        // Arrange
        var endpoint1 = CreateEndpoint("a/{*b}", order: 0);
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("{a}/{b}", order: 1);
        builder.AddEndpoint(endpoint2);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a1 = next.Value;
        Assert.Same(endpoint1, Assert.Single(a1.Matches));
        Assert.Null(a1.Literals);

        var b1 = a1.Parameters;
        Assert.Same(endpoint2, Assert.Single(b1.Matches));
        Assert.Null(b1.Literals);
        Assert.Null(b1.Parameters);
        Assert.Null(b1.CatchAll);

        var a2 = root.Parameters;
        Assert.Null(a2.Matches);
        Assert.Null(a2.Literals);

        var b2 = a2.Parameters;
        Assert.Collection(
            b2.Matches,
            e => Assert.Same(endpoint2, e));
        Assert.Null(b2.Literals);
        Assert.Null(b2.Parameters);
        Assert.Null(b2.CatchAll);
    }

    [Fact]
    public void BuildDfaTree_WithPolicies()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

        var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(0), new TestMetadata2(true), });
        builder.AddEndpoint(endpoint1);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Empty(a.Matches);
        Assert.IsType<TestMetadata1MatcherPolicy>(a.NodeBuilder);
        Assert.Collection(
            a.PolicyEdges.OrderBy(e => e.Key),
            e => Assert.Equal(0, e.Key));

        var test1_0 = a.PolicyEdges[0];
        Assert.Empty(a.Matches);
        Assert.IsType<TestMetadata2MatcherPolicy>(test1_0.NodeBuilder);
        Assert.Collection(
            test1_0.PolicyEdges.OrderBy(e => e.Key),
            e => Assert.Equal(true, e.Key));

        var test2_true = test1_0.PolicyEdges[true];
        Assert.Same(endpoint1, Assert.Single(test2_true.Matches));
        Assert.Null(test2_true.NodeBuilder);
        Assert.Null(test2_true.PolicyEdges);
    }

    [Fact]
    public void BuildDfaTree_WithPolicies_AndBranches()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

        var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(0), new TestMetadata2(true), });
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), new TestMetadata2(true), });
        builder.AddEndpoint(endpoint2);

        var endpoint3 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), new TestMetadata2(false), });
        builder.AddEndpoint(endpoint3);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Empty(a.Matches);
        Assert.IsType<TestMetadata1MatcherPolicy>(a.NodeBuilder);
        Assert.Collection(
            a.PolicyEdges.OrderBy(e => e.Key),
            e => Assert.Equal(0, e.Key),
            e => Assert.Equal(1, e.Key));

        var test1_0 = a.PolicyEdges[0];
        Assert.Empty(test1_0.Matches);
        Assert.IsType<TestMetadata2MatcherPolicy>(test1_0.NodeBuilder);
        Assert.Collection(
            test1_0.PolicyEdges.OrderBy(e => e.Key),
            e => Assert.Equal(true, e.Key));

        var test2_true = test1_0.PolicyEdges[true];
        Assert.Same(endpoint1, Assert.Single(test2_true.Matches));
        Assert.Null(test2_true.NodeBuilder);
        Assert.Null(test2_true.PolicyEdges);

        var test1_1 = a.PolicyEdges[1];
        Assert.Empty(test1_1.Matches);
        Assert.IsType<TestMetadata2MatcherPolicy>(test1_1.NodeBuilder);
        Assert.Collection(
            test1_1.PolicyEdges.OrderBy(e => e.Key),
            e => Assert.Equal(false, e.Key),
            e => Assert.Equal(true, e.Key));

        test2_true = test1_1.PolicyEdges[true];
        Assert.Same(endpoint2, Assert.Single(test2_true.Matches));
        Assert.Null(test2_true.NodeBuilder);
        Assert.Null(test2_true.PolicyEdges);

        var test2_false = test1_1.PolicyEdges[false];
        Assert.Same(endpoint3, Assert.Single(test2_false.Matches));
        Assert.Null(test2_false.NodeBuilder);
        Assert.Null(test2_false.PolicyEdges);
    }

    [Fact]
    public void BuildDfaTree_WithPolicies_AndBranches_FirstPolicySkipped()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

        var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata2(true), });
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata2(true), });
        builder.AddEndpoint(endpoint2);

        var endpoint3 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata2(false), });
        builder.AddEndpoint(endpoint3);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Empty(a.Matches);
        Assert.IsType<TestMetadata2MatcherPolicy>(a.NodeBuilder);
        Assert.Collection(
            a.PolicyEdges.OrderBy(e => e.Key),
            e => Assert.Equal(false, e.Key),
            e => Assert.Equal(true, e.Key));

        var test2_true = a.PolicyEdges[true];
        Assert.Equal(new[] { endpoint1, endpoint2, }, test2_true.Matches);
        Assert.Null(test2_true.NodeBuilder);
        Assert.Null(test2_true.PolicyEdges);

        var test2_false = a.PolicyEdges[false];
        Assert.Equal(new[] { endpoint3, }, test2_false.Matches);
        Assert.Null(test2_false.NodeBuilder);
        Assert.Null(test2_false.PolicyEdges);
    }

    [Fact]
    public void BuildDfaTree_WithPolicies_AndBranches_SecondSkipped()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

        var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(0), });
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), });
        builder.AddEndpoint(endpoint2);

        var endpoint3 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), });
        builder.AddEndpoint(endpoint3);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Empty(a.Matches);
        Assert.IsType<TestMetadata1MatcherPolicy>(a.NodeBuilder);
        Assert.Collection(
            a.PolicyEdges.OrderBy(e => e.Key),
            e => Assert.Equal(0, e.Key),
            e => Assert.Equal(1, e.Key));

        var test1_0 = a.PolicyEdges[0];
        Assert.Equal(new[] { endpoint1, }, test1_0.Matches);
        Assert.Null(test1_0.NodeBuilder);
        Assert.Null(test1_0.PolicyEdges);

        var test1_1 = a.PolicyEdges[1];
        Assert.Equal(new[] { endpoint2, endpoint3, }, test1_1.Matches);
        Assert.Null(test1_1.NodeBuilder);
        Assert.Null(test1_1.PolicyEdges);
    }

    [Fact]
    public void BuildDfaTree_WithPolicies_AndBranches_NonRouteEndpoint()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder(new TestNonRoutePatternMatcherPolicy());

        var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(0), });
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), });
        builder.AddEndpoint(endpoint2);

        var endpoint3 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), });
        builder.AddEndpoint(endpoint3);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Empty(a.Matches);
        Assert.IsType<TestNonRoutePatternMatcherPolicy>(a.NodeBuilder);
        Assert.Collection(
            a.PolicyEdges.OrderBy(e => e.Key),
            e => Assert.Equal(0, e.Key),
            e => Assert.Equal(1, e.Key),
            e => Assert.Equal(int.MaxValue, e.Key));

        var test1_0 = a.PolicyEdges[0];
        Assert.Equal(new[] { endpoint1, }, test1_0.Matches);
        Assert.Null(test1_0.NodeBuilder);
        Assert.Null(test1_0.PolicyEdges);

        var test1_1 = a.PolicyEdges[1];
        Assert.Equal(new[] { endpoint2, endpoint3, }, test1_1.Matches);
        Assert.Null(test1_1.NodeBuilder);
        Assert.Null(test1_1.PolicyEdges);

        var nonRouteEndpoint = a.PolicyEdges[int.MaxValue];
        Assert.Equal("MaxValueEndpoint", Assert.Single(nonRouteEndpoint.Matches).DisplayName);
    }

    [Fact]
    public void BuildDfaTree_WithPolicies_AndBranches_BothPoliciesSkipped()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

        var endpoint1 = CreateEndpoint("/a", metadata: new object[] { });
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("/a", metadata: new object[] { });
        builder.AddEndpoint(endpoint2);

        var endpoint3 = CreateEndpoint("/a", metadata: new object[] { });
        builder.AddEndpoint(endpoint3);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("a", next.Key);

        var a = next.Value;
        Assert.Equal(new[] { endpoint1, endpoint2, endpoint3, }, a.Matches);
        Assert.Null(a.NodeBuilder);
        Assert.Null(a.PolicyEdges);
    }

    // Verifies that we sort the endpoints before calling into policies.
    //
    // The builder uses a different sort order when building the tree, vs when building the policy nodes. Policy
    // nodes should see an "absolute" order.
    [Fact]
    public void BuildDfaTree_WithPolicies_SortedAccordingToScore()
    {
        // Arrange
        //
        // These cases where chosen where the absolute order incontrolled explicitly by setting .Order, but
        // the precedence of segments is different, so these will be sorted into different orders when building
        // the tree.
        var policies = new MatcherPolicy[]
        {
                new TestMetadata1MatcherPolicy(),
                new TestMetadata2MatcherPolicy(),
        };

        var comparer = new EndpointComparer(policies.OrderBy(p => p.Order).OfType<IEndpointComparerPolicy>().ToArray());

        var builder = CreateDfaMatcherBuilder(policies);

        ((TestMetadata1MatcherPolicy)policies[0]).OnGetEdges = VerifyOrder;
        ((TestMetadata2MatcherPolicy)policies[1]).OnGetEdges = VerifyOrder;

        var endpoint1 = CreateEndpoint("/a/{**b}", order: -1, metadata: new object[] { new TestMetadata1(0), new TestMetadata2(true), });
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateEndpoint("/a/{b}/{**c}", order: 0, metadata: new object[] { new TestMetadata1(1), new TestMetadata2(true), });
        builder.AddEndpoint(endpoint2);

        var endpoint3 = CreateEndpoint("/a/b/{c}", order: 1, metadata: new object[] { new TestMetadata1(1), new TestMetadata2(false), });
        builder.AddEndpoint(endpoint3);

        // Act & Assert
        _ = builder.BuildDfaTree();

        void VerifyOrder(IReadOnlyList<Endpoint> endpoints)
        {
            // The list should already be in sorted order, every time build is called.
            Assert.Equal(endpoints, endpoints.OrderBy(e => e, comparer));
        }
    }

    [Fact]
    public void BuildDfaTree_RequiredValues()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint = CreateEndpoint("{controller}/{action}", requiredValues: new { controller = "Home", action = "Index" });
        builder.AddEndpoint(endpoint);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("Home", next.Key);

        var home = next.Value;
        Assert.Null(home.Matches);
        Assert.Null(home.Parameters);

        next = Assert.Single(home.Literals);
        Assert.Equal("Index", next.Key);

        var index = next.Value;
        Assert.Same(endpoint, Assert.Single(index.Matches));
        Assert.Null(index.Literals);
    }

    [Fact]
    public void BuildDfaTree_RequiredValues_AndMatchingDefaults()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint = CreateEndpoint(
            "{controller}/{action}",
            defaults: new { controller = "Home", action = "Index" },
            requiredValues: new { controller = "Home", action = "Index" });
        builder.AddEndpoint(endpoint);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Same(endpoint, Assert.Single(root.Matches));
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("Home", next.Key);

        var home = next.Value;
        Assert.Same(endpoint, Assert.Single(home.Matches));
        Assert.Null(home.Parameters);

        next = Assert.Single(home.Literals);
        Assert.Equal("Index", next.Key);

        var index = next.Value;
        Assert.Same(endpoint, Assert.Single(index.Matches));
        Assert.Null(index.Literals);
    }

    [Fact]
    public void BuildDfaTree_RequiredValues_AndDifferentDefaults()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint = CreateSubsitutedEndpoint(
            "{controller}/{action}",
            defaults: new { controller = "Home", action = "Index" },
            requiredValues: new { controller = "Login", action = "Index" });
        builder.AddEndpoint(endpoint);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("Login", next.Key);

        var login = next.Value;
        Assert.Same(endpoint, Assert.Single(login.Matches));
        Assert.Null(login.Parameters);

        next = Assert.Single(login.Literals);
        Assert.Equal("Index", next.Key);

        var index = next.Value;
        Assert.Same(endpoint, Assert.Single(index.Matches));
        Assert.Null(index.Literals);
    }

    [Fact]
    public void BuildDfaTree_RequiredValues_Multiple()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint1 = CreateSubsitutedEndpoint(
            "{controller}/{action}/{id?}",
            defaults: new { controller = "Home", action = "Index" },
            requiredValues: new { controller = "Home", action = "Index" });
        builder.AddEndpoint(endpoint1);

        var endpoint2 = CreateSubsitutedEndpoint(
            "{controller}/{action}/{id?}",
            defaults: new { controller = "Home", action = "Index" },
            requiredValues: new { controller = "Login", action = "Index" });
        builder.AddEndpoint(endpoint2);

        var endpoint3 = CreateSubsitutedEndpoint(
            "{controller}/{action}/{id?}",
            defaults: new { controller = "Home", action = "Index" },
            requiredValues: new { controller = "Login", action = "ChangePassword" });
        builder.AddEndpoint(endpoint3);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Same(endpoint1, Assert.Single(root.Matches));
        Assert.Null(root.Parameters);

        Assert.Equal(2, root.Literals.Count);

        var home = root.Literals["Home"];

        Assert.Same(endpoint1, Assert.Single(home.Matches));
        Assert.Null(home.Parameters);

        var next = Assert.Single(home.Literals);
        Assert.Equal("Index", next.Key);

        var homeIndex = next.Value;
        Assert.Same(endpoint1, Assert.Single(homeIndex.Matches));
        Assert.Null(homeIndex.Literals);
        Assert.NotNull(homeIndex.Parameters);

        Assert.Same(endpoint1, Assert.Single(homeIndex.Parameters.Matches));

        var login = root.Literals["Login"];

        Assert.Same(endpoint2, Assert.Single(login.Matches));
        Assert.Null(login.Parameters);

        Assert.Equal(2, login.Literals.Count);

        var loginIndex = login.Literals["Index"];

        Assert.Same(endpoint2, Assert.Single(loginIndex.Matches));
        Assert.Null(loginIndex.Literals);
        Assert.NotNull(loginIndex.Parameters);

        Assert.Same(endpoint2, Assert.Single(loginIndex.Parameters.Matches));

        var loginChangePassword = login.Literals["ChangePassword"];

        Assert.Same(endpoint3, Assert.Single(loginChangePassword.Matches));
        Assert.Null(loginChangePassword.Literals);
        Assert.NotNull(loginChangePassword.Parameters);

        Assert.Same(endpoint3, Assert.Single(loginChangePassword.Parameters.Matches));
    }

    [Fact]
    public void BuildDfaTree_RequiredValues_AndParameterTransformer()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint = CreateEndpoint(
            "{controller:slugify}/{action:slugify}",
            defaults: new { controller = "RecentProducts", action = "ViewAll" },
            requiredValues: new { controller = "RecentProducts", action = "ViewAll" });
        builder.AddEndpoint(endpoint);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Same(endpoint, Assert.Single(root.Matches));
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("recent-products", next.Key);

        var home = next.Value;
        Assert.Same(endpoint, Assert.Single(home.Matches));
        Assert.Null(home.Parameters);

        next = Assert.Single(home.Literals);
        Assert.Equal("view-all", next.Key);

        var index = next.Value;
        Assert.Same(endpoint, Assert.Single(index.Matches));
        Assert.Null(index.Literals);
    }

    [Fact]
    public void BuildDfaTree_RequiredValues_AndDefaults_AndParameterTransformer()
    {
        // Arrange
        var builder = CreateDfaMatcherBuilder();

        var endpoint = CreateEndpoint(
            "ConventionalTransformerRoute/{controller:slugify}/{action=Index}/{param:slugify?}",
            requiredValues: new { controller = "ConventionalTransformer", action = "Index", area = (string)null, page = (string)null });
        builder.AddEndpoint(endpoint);

        // Act
        var root = builder.BuildDfaTree();

        // Assert
        Assert.Null(root.Matches);
        Assert.Null(root.Parameters);

        var next = Assert.Single(root.Literals);
        Assert.Equal("ConventionalTransformerRoute", next.Key);

        var conventionalTransformerRoute = next.Value;
        Assert.Null(conventionalTransformerRoute.Matches);
        Assert.Null(conventionalTransformerRoute.Parameters);

        next = Assert.Single(conventionalTransformerRoute.Literals);
        Assert.Equal("conventional-transformer", next.Key);

        var conventionalTransformer = next.Value;
        Assert.Same(endpoint, Assert.Single(conventionalTransformer.Matches));

        next = Assert.Single(conventionalTransformer.Literals);
        Assert.Equal("Index", next.Key);

        var index = next.Value;
        Assert.Same(endpoint, Assert.Single(index.Matches));

        Assert.NotNull(index.Parameters);

        Assert.Same(endpoint, Assert.Single(index.Parameters.Matches));
    }

    [Fact]
    public void CreateCandidate_JustLiterals()
    {
        // Arrange
        var endpoint = CreateEndpoint("/a/b/c");

        var builder = CreateDfaMatcherBuilder();

        // Act
        var candidate = builder.CreateCandidate(endpoint, score: 0);

        // Assert
        Assert.Equal(Candidate.CandidateFlags.None, candidate.Flags);
        Assert.Empty(candidate.Slots);
        Assert.Empty(candidate.Captures);
        Assert.Equal(default, candidate.CatchAll);
        Assert.Empty(candidate.ComplexSegments);
        Assert.Empty(candidate.Constraints);
    }

    [Fact]
    public void CreateCandidate_Parameters()
    {
        // Arrange
        var endpoint = CreateEndpoint("/{a}/{b}/{c}");

        var builder = CreateDfaMatcherBuilder();

        // Act
        var candidate = builder.CreateCandidate(endpoint, score: 0);

        // Assert
        Assert.Equal(Candidate.CandidateFlags.HasCaptures, candidate.Flags);
        Assert.Equal(3, candidate.Slots.Length);
        Assert.Collection(
            candidate.Captures,
            c => Assert.Equal(("a", 0, 0), c),
            c => Assert.Equal(("b", 1, 1), c),
            c => Assert.Equal(("c", 2, 2), c));
        Assert.Equal(default, candidate.CatchAll);
        Assert.Empty(candidate.ComplexSegments);
        Assert.Empty(candidate.Constraints);
    }

    [Fact]
    public void CreateCandidate_Parameters_WithDefaults()
    {
        // Arrange
        var endpoint = CreateEndpoint("/{a=aa}/{b=bb}/{c=cc}");

        var builder = CreateDfaMatcherBuilder();

        // Act
        var candidate = builder.CreateCandidate(endpoint, score: 0);

        // Assert
        Assert.Equal(
            Candidate.CandidateFlags.HasDefaults | Candidate.CandidateFlags.HasCaptures,
            candidate.Flags);
        Assert.Collection(
            candidate.Slots,
            s => Assert.Equal(new KeyValuePair<string, object>("a", "aa"), s),
            s => Assert.Equal(new KeyValuePair<string, object>("b", "bb"), s),
            s => Assert.Equal(new KeyValuePair<string, object>("c", "cc"), s));
        Assert.Collection(
            candidate.Captures,
            c => Assert.Equal(("a", 0, 0), c),
            c => Assert.Equal(("b", 1, 1), c),
            c => Assert.Equal(("c", 2, 2), c));
        Assert.Equal(default, candidate.CatchAll);
        Assert.Empty(candidate.ComplexSegments);
        Assert.Empty(candidate.Constraints);
    }

    [Fact]
    public void CreateCandidate_Parameters_CatchAll()
    {
        // Arrange
        var endpoint = CreateEndpoint("/{a}/{b}/{*c=cc}");

        var builder = CreateDfaMatcherBuilder();

        // Act
        var candidate = builder.CreateCandidate(endpoint, score: 0);

        // Assert
        Assert.Equal(
            Candidate.CandidateFlags.HasDefaults |
                Candidate.CandidateFlags.HasCaptures |
                Candidate.CandidateFlags.HasCatchAll,
            candidate.Flags);
        Assert.Collection(
            candidate.Slots,
            s => Assert.Equal(new KeyValuePair<string, object>("c", "cc"), s),
            s => Assert.Equal(new KeyValuePair<string, object>(null, null), s),
            s => Assert.Equal(new KeyValuePair<string, object>(null, null), s));
        Assert.Collection(
            candidate.Captures,
            c => Assert.Equal(("a", 0, 1), c),
            c => Assert.Equal(("b", 1, 2), c));
        Assert.Equal(("c", 2, 0), candidate.CatchAll);
        Assert.Empty(candidate.ComplexSegments);
        Assert.Empty(candidate.Constraints);
    }

    // Defaults are processed first, which affects the slot ordering.
    [Fact]
    public void CreateCandidate_Parameters_OutOfLineDefaults()
    {
        // Arrange
        var endpoint = CreateEndpoint("/{a}/{b}/{c=cc}", new { a = "aa", d = "dd", });

        var builder = CreateDfaMatcherBuilder();

        // Act
        var candidate = builder.CreateCandidate(endpoint, score: 0);

        // Assert
        Assert.Equal(
            Candidate.CandidateFlags.HasDefaults | Candidate.CandidateFlags.HasCaptures,
            candidate.Flags);
        Assert.Collection(
            candidate.Slots,
            s => Assert.Equal(new KeyValuePair<string, object>("a", "aa"), s),
            s => Assert.Equal(new KeyValuePair<string, object>("d", "dd"), s),
            s => Assert.Equal(new KeyValuePair<string, object>("c", "cc"), s),
            s => Assert.Equal(new KeyValuePair<string, object>(null, null), s));
        Assert.Collection(
            candidate.Captures,
            c => Assert.Equal(("a", 0, 0), c),
            c => Assert.Equal(("b", 1, 3), c),
            c => Assert.Equal(("c", 2, 2), c));
        Assert.Equal(default, candidate.CatchAll);
        Assert.Empty(candidate.ComplexSegments);
        Assert.Empty(candidate.Constraints);
    }

    [Fact]
    public void CreateCandidate_Parameters_ComplexSegments()
    {
        // Arrange
        var endpoint = CreateEndpoint("/{a}-{b=bb}/{c}");

        var builder = CreateDfaMatcherBuilder();

        // Act
        var candidate = builder.CreateCandidate(endpoint, score: 0);

        // Assert
        Assert.Equal(
            Candidate.CandidateFlags.HasDefaults |
                Candidate.CandidateFlags.HasCaptures |
                Candidate.CandidateFlags.HasComplexSegments,
            candidate.Flags);
        Assert.Collection(
            candidate.Slots,
            s => Assert.Equal(new KeyValuePair<string, object>("b", "bb"), s),
            s => Assert.Equal(new KeyValuePair<string, object>(null, null), s));
        Assert.Collection(
            candidate.Captures,
            c => Assert.Equal(("c", 1, 1), c));
        Assert.Equal(default, candidate.CatchAll);
        Assert.Collection(
            candidate.ComplexSegments,
            s => Assert.Equal(0, s.segmentIndex));
        Assert.Empty(candidate.Constraints);
    }

    [Fact]
    public void CreateCandidate_RouteConstraints()
    {
        // Arrange
        var endpoint = CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), });

        var builder = CreateDfaMatcherBuilder();

        // Act
        var candidate = builder.CreateCandidate(endpoint, score: 0);

        // Assert
        Assert.Equal(Candidate.CandidateFlags.HasConstraints, candidate.Flags);
        Assert.Empty(candidate.Slots);
        Assert.Empty(candidate.Captures);
        Assert.Equal(default, candidate.CatchAll);
        Assert.Empty(candidate.ComplexSegments);
        Assert.Single(candidate.Constraints);
    }

    [Fact]
    public void CreateCandidate_InlineRouteConstraints_Duplicate_SameInstance()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/a/b/{c:int}");
        var endpoint2 = CreateEndpoint("/d/e/{f:int}");

        var builder = CreateDfaMatcherBuilder();

        // Act
        var candidate1 = builder.CreateCandidate(endpoint1, score: 0);
        var candidate2 = builder.CreateCandidate(endpoint2, score: 0);

        // Assert
        Assert.Equal(Candidate.CandidateFlags.HasConstraints | Candidate.CandidateFlags.HasCaptures, candidate1.Flags);
        var constraint1 = Assert.Single(candidate1.Constraints);

        Assert.Equal(Candidate.CandidateFlags.HasConstraints | Candidate.CandidateFlags.HasCaptures, candidate2.Flags);
        var constraint2 = Assert.Single(candidate2.Constraints);

        Assert.Same(constraint1.Value, constraint2.Value);
    }

    [Fact]
    public void CreateCandidate_InlineRouteConstraintsWithArgument_Duplicate_SameInstance()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/a/b/{c:regex([A-Z])}");
        var endpoint2 = CreateEndpoint("/d/e/{f:regex([A-Z])}");

        var builder = CreateDfaMatcherBuilder();

        // Act
        var candidate1 = builder.CreateCandidate(endpoint1, score: 0);
        var candidate2 = builder.CreateCandidate(endpoint2, score: 0);

        // Assert
        Assert.Equal(Candidate.CandidateFlags.HasConstraints | Candidate.CandidateFlags.HasCaptures, candidate1.Flags);
        var constraint1 = Assert.Single(candidate1.Constraints);

        Assert.Equal(Candidate.CandidateFlags.HasConstraints | Candidate.CandidateFlags.HasCaptures, candidate2.Flags);
        var constraint2 = Assert.Single(candidate2.Constraints);

        Assert.Same(constraint1.Value, constraint2.Value);
    }

    [Fact]
    public void CreateCandidate_InlineRouteConstraintsWithArgument_DifferentArgument_DifferentInstance()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/a/b/{c:regex([A-Z])}");
        var endpoint2 = CreateEndpoint("/d/e/{f:regex([a-z])}");

        var builder = CreateDfaMatcherBuilder();

        // Act
        var candidate1 = builder.CreateCandidate(endpoint1, score: 0);
        var candidate2 = builder.CreateCandidate(endpoint2, score: 0);

        // Assert
        Assert.Equal(Candidate.CandidateFlags.HasConstraints | Candidate.CandidateFlags.HasCaptures, candidate1.Flags);
        var constraint1 = Assert.Single(candidate1.Constraints);

        Assert.Equal(Candidate.CandidateFlags.HasConstraints | Candidate.CandidateFlags.HasCaptures, candidate2.Flags);
        var constraint2 = Assert.Single(candidate2.Constraints);

        Assert.NotSame(constraint1.Value, constraint2.Value);
    }

    [Fact]
    public void CreateCandidate_CustomParameterPolicy()
    {
        // Arrange
        var endpoint = CreateEndpoint("/a/b/c", constraints: new { a = new CustomParameterPolicy(), });

        var builder = CreateDfaMatcherBuilder();

        // Act
        var candidate = builder.CreateCandidate(endpoint, score: 0);

        // Assert
        Assert.Equal(Candidate.CandidateFlags.None, candidate.Flags);
        Assert.Empty(candidate.Slots);
        Assert.Empty(candidate.Captures);
        Assert.Equal(default, candidate.CatchAll);
        Assert.Empty(candidate.ComplexSegments);
        Assert.Empty(candidate.Constraints);
    }

    private class CustomParameterPolicy : IParameterPolicy
    {
    }

    [Fact]
    public void CreateCandidates_CreatesScoresCorrectly()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), }, metadata: new object[] { new TestMetadata1(), new TestMetadata2(), }),
                CreateEndpoint("/a/b/c", constraints: new { a = new AlphaRouteConstraint(), }, metadata: new object[] { new TestMetadata1(), new TestMetadata2(), }),
                CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), }, metadata: new object[] { new TestMetadata1(), }),
                CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), }, metadata: new object[] { new TestMetadata2(), }),
                CreateEndpoint("/a/b/c", constraints: new { }, metadata: new object[] { }),
                CreateEndpoint("/a/b/c", constraints: new { }, metadata: new object[] { }),
            };

        var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

        // Act
        var candidates = builder.CreateCandidates(endpoints);

        // Assert
        Assert.Collection(
            candidates,
            c => Assert.Equal(0, c.Score),
            c => Assert.Equal(0, c.Score),
            c => Assert.Equal(1, c.Score),
            c => Assert.Equal(2, c.Score),
            c => Assert.Equal(3, c.Score),
            c => Assert.Equal(3, c.Score));
    }

    private static DfaMatcherBuilder CreateDfaMatcherBuilder(params MatcherPolicy[] policies)
    {
        var policyFactory = CreateParameterPolicyFactory();
        var dataSource = new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>());
        return new DfaMatcherBuilder(
            NullLoggerFactory.Instance,
            policyFactory,
            Mock.Of<EndpointSelector>(),
            policies);
    }

    private static RouteEndpoint CreateSubsitutedEndpoint(
        string template,
        object defaults = null,
        object constraints = null,
        object requiredValues = null,
        params object[] metadata)
    {
        var routePattern = RoutePatternFactory.Parse(template, defaults, constraints);

        var policyFactory = CreateParameterPolicyFactory();
        var defaultRoutePatternTransformer = new DefaultRoutePatternTransformer(policyFactory);

        routePattern = defaultRoutePatternTransformer.SubstituteRequiredValues(routePattern, requiredValues);

        return EndpointFactory.CreateRouteEndpoint(routePattern, metadata: metadata);
    }

    public static RoutePattern CreateRoutePattern(RoutePattern routePattern, object requiredValues)
    {
        if (requiredValues != null)
        {
            var policyFactory = CreateParameterPolicyFactory();
            var defaultRoutePatternTransformer = new DefaultRoutePatternTransformer(policyFactory);

            routePattern = defaultRoutePatternTransformer.SubstituteRequiredValues(routePattern, requiredValues);
        }

        return routePattern;
    }

    private static DefaultParameterPolicyFactory CreateParameterPolicyFactory()
    {
        var serviceCollection = new ServiceCollection();
        var policyFactory = new DefaultParameterPolicyFactory(
            Options.Create(new RouteOptions
            {
                ConstraintMap =
                {
                        ["slugify"] = typeof(SlugifyParameterTransformer),
                        ["upper-case"] = typeof(UpperCaseParameterTransform),
                        ["regex"] = typeof(RegexInlineRouteConstraint) // Regex not included by default since introduction of CreateSlimBuilder
                }
            }),
            serviceCollection.BuildServiceProvider());

        return policyFactory;
    }

    private static RouteEndpoint CreateEndpoint(
        string template,
        object defaults = null,
        object constraints = null,
        object requiredValues = null,
        int order = 0,
        params object[] metadata)
    {
        return EndpointFactory.CreateRouteEndpoint(template, defaults, constraints, requiredValues, order: order, metadata: metadata);
    }

    private class TestMetadata1
    {
        public TestMetadata1()
        {
        }

        public TestMetadata1(int state)
        {
            State = state;
        }

        public int State { get; set; }
    }

    private class TestMetadata1MatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy
    {
        public override int Order => 100;

        public IComparer<Endpoint> Comparer => EndpointMetadataComparer<TestMetadata1>.Default;

        public Action<IReadOnlyList<Endpoint>> OnGetEdges { get; set; }

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            return endpoints.Any(e => e.Metadata.GetMetadata<TestMetadata1>() != null);
        }

        public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
        {
            OnGetEdges?.Invoke(endpoints);
            return endpoints
                .GroupBy(e => e.Metadata.GetMetadata<TestMetadata1>().State)
                .Select(g => new PolicyNodeEdge(g.Key, g.ToArray()))
                .ToArray();
        }
    }

    private class TestMetadata2
    {
        public TestMetadata2()
        {
        }

        public TestMetadata2(bool state)
        {
            State = state;
        }

        public bool State { get; set; }
    }

    private class TestMetadata2MatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy
    {
        public override int Order => 101;

        public IComparer<Endpoint> Comparer => EndpointMetadataComparer<TestMetadata2>.Default;

        public Action<IReadOnlyList<Endpoint>> OnGetEdges { get; set; }

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            return endpoints.Any(e => e.Metadata.GetMetadata<TestMetadata2>() != null);
        }

        public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
        {
            OnGetEdges?.Invoke(endpoints);
            return endpoints
                .GroupBy(e => e.Metadata.GetMetadata<TestMetadata2>().State)
                .Select(g => new PolicyNodeEdge(g.Key, g.ToArray()))
                .ToArray();
        }
    }

    private class TestNonRoutePatternMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy
    {
        public override int Order => 100;

        public IComparer<Endpoint> Comparer => EndpointMetadataComparer<TestMetadata1>.Default;

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            return endpoints.Any(e => e.Metadata.GetMetadata<TestMetadata1>() != null);
        }

        public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
        {
            var edges = endpoints
                .GroupBy(e => e.Metadata.GetMetadata<TestMetadata1>().State)
                .Select(g => new PolicyNodeEdge(g.Key, g.ToArray()))
                .ToList();

            var maxValueEndpoint = new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "MaxValueEndpoint");
            edges.Add(new PolicyNodeEdge(int.MaxValue, new[] { maxValueEndpoint }));

            return edges;
        }
    }
}
