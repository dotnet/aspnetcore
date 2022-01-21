// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template;

public class TemplateSegmentTest
{
    [Fact]
    public void Ctor_RoutePatternPathSegment_ShouldThrowArgumentNullExceptionWhenOtherIsNull()
    {
        const RoutePatternPathSegment other = null;

        var actual = Assert.ThrowsAny<ArgumentNullException>(() => new TemplateSegment(other));
        Assert.Equal(nameof(other), actual.ParamName);
    }

    [Fact]
    public void ToRoutePatternPathSegment()
    {
        // Arrange
        var literalPartA = RoutePatternFactory.LiteralPart("A");
        var paramPartB = RoutePatternFactory.ParameterPart("B");
        var paramPartC = RoutePatternFactory.ParameterPart("C");
        var paramPartD = RoutePatternFactory.ParameterPart("D");
        var separatorPartE = RoutePatternFactory.SeparatorPart("E");
        var templateSegment = new TemplateSegment(RoutePatternFactory.Segment(paramPartC, literalPartA, separatorPartE, paramPartB));

        // Act
        var routePatternPathSegment = templateSegment.ToRoutePatternPathSegment();
        templateSegment.Parts[1] = new TemplatePart(RoutePatternFactory.ParameterPart("D"));
        templateSegment.Parts.RemoveAt(0);

        // Assert
        Assert.Equal(4, routePatternPathSegment.Parts.Count);
        Assert.IsType<RoutePatternParameterPart>(routePatternPathSegment.Parts[0]);
        Assert.Equal(paramPartC.Name, ((RoutePatternParameterPart)routePatternPathSegment.Parts[0]).Name);
        Assert.IsType<RoutePatternLiteralPart>(routePatternPathSegment.Parts[1]);
        Assert.Equal(literalPartA.Content, ((RoutePatternLiteralPart)routePatternPathSegment.Parts[1]).Content);
        Assert.IsType<RoutePatternSeparatorPart>(routePatternPathSegment.Parts[2]);
        Assert.Equal(separatorPartE.Content, ((RoutePatternSeparatorPart)routePatternPathSegment.Parts[2]).Content);
        Assert.IsType<RoutePatternParameterPart>(routePatternPathSegment.Parts[3]);
        Assert.Equal(paramPartB.Name, ((RoutePatternParameterPart)routePatternPathSegment.Parts[3]).Name);
    }
}
