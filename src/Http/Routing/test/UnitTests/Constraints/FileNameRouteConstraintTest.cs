// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Constraints;

public class FileNameRouteConstraintTest
{
    public static TheoryData<object> FileNameData
    {
        get
        {
            return new TheoryData<object>()
                {
                    "hello.txt",
                    "hello.txt.jpg",
                    "/hello.t",
                    "/////hello.x",
                    "a/b/c/d.e",
                    "a/b./.c/d.e",
                    ".gitnore",
                    ".a",
                    "/.......a"
                };
        }
    }

    [Theory]
    [MemberData(nameof(FileNameData))]
    public void Match_RouteValue_IsFileName(object value)
    {
        // Arrange
        var constraint = new FileNameRouteConstraint();

        var values = new RouteValueDictionary();
        values.Add("path", value);

        // Act
        var result = constraint.Match(httpContext: null, route: null, "path", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.True(result);
    }

    public static TheoryData<object> NonFileNameData
    {
        get
        {
            return new TheoryData<object>()
                {
                    null,
                    string.Empty,
                    "/",
                    ".",
                    "..........",
                    "hello.",
                    "/hello",
                    "//",
                    "//b.c/",
                    "/////hello.",
                    "a/b./.c/d.",
                };
        }
    }

    [Theory]
    [MemberData(nameof(NonFileNameData))]
    public void Match_RouteValue_IsNotFileName(object value)
    {
        // Arrange
        var constraint = new FileNameRouteConstraint();

        var values = new RouteValueDictionary();
        values.Add("path", value);

        // Act
        var result = constraint.Match(httpContext: null, route: null, "path", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Match_MissingValue_IsNotFileName()
    {
        // Arrange
        var constraint = new FileNameRouteConstraint();

        var values = new RouteValueDictionary();

        // Act
        var result = constraint.Match(httpContext: null, route: null, "path", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.False(result);
    }
}
