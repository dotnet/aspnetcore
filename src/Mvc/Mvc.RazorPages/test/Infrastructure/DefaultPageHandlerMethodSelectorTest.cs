// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class DefaultPageHandlerMethodSelectorTest
{
    [Fact]
    public void NewBehavior_Select_ReturnsFuzzyMatchForHead_WhenNoHeadHandlerDefined()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "GET"
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST"
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                    },
            },
            RouteData = new RouteData(),
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "HEAD"
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Same(descriptor1, actual);
    }

    [Fact]
    public void NewBehavior_Select_PrefersExactMatch()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "GET"
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST"
        };

        var descriptor3 = new HandlerMethodDescriptor
        {
            HttpMethod = "HEAD"
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                        descriptor3,
                    },
            },
            RouteData = new RouteData(),
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "HEAD",
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Same(descriptor3, actual);
    }

    [Fact]
    public void NewBehavior_Select_PrefersExactMatch_ReturnsNullWhenHandlerNameDoesntMatch()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "GET"
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST"
        };

        // This will match the HTTP method 'round' of selection, but won't match the
        // handler name.
        var descriptor3 = new HandlerMethodDescriptor
        {
            HttpMethod = "HEAD",
            Name = "not-provided",
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                        descriptor3,
                    },
            },
            RouteData = new RouteData(),
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "HEAD",
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Null(actual);
    }

    [Theory]
    [InlineData("HEAD")]
    [InlineData("heAd")]
    public void NewBehavior_Select_ReturnsFuzzyMatch_SafeVerbs(string httpMethod)
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "GET"
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST"
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                    },
            },
            RouteData = new RouteData(),
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = httpMethod
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Same(descriptor1, actual);
    }

    [Fact]
    public void Select_ReturnsOnlyHandler()
    {
        // Arrange
        var descriptor = new HandlerMethodDescriptor
        {
            HttpMethod = "GET"
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor,
                    },
            },
            RouteData = new RouteData(),
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "GET"
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Same(descriptor, actual);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public void Select_ReturnsHandlerWithMatchingHttpRequestMethod(string httpMethod)
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "PUT",
        };
        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = httpMethod,
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                    },
            },
            RouteData = new RouteData(),
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = httpMethod,
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Same(descriptor2, actual);
    }

    [Fact]
    public void Select_ReturnsNullWhenNoHandlerMatchesHandler()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Add",
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Delete",
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                    },
            },
            RouteData = new RouteData
            {
                Values =
                    {
                        { "handler", "update" }
                    }
            },
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "POST"
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void Select_ReturnsHandlerThatMatchesHandler()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Add",
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Delete",
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                    },
            },
            RouteData = new RouteData
            {
                Values =
                    {
                        { "handler", "Add" }
                    }
            },
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "Post"
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Same(descriptor1, actual);
    }

    [Fact]
    [ReplaceCulture("de-CH", "de-CH")]
    public void Select_ReturnsHandlerThatMatchesHandler_UsesInvariantCulture()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "10/31/2018 07:37:38 -07:00",
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Delete",
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                    },
            },
            RouteData = new RouteData
            {
                Values =
                    {
                        { "handler", new DateTimeOffset(2018, 10, 31, 7, 37, 38, TimeSpan.FromHours(-7)) },
                    }
            },
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "Post"
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Same(descriptor1, actual);
    }

    [Fact]
    public void Select_HandlerFromQueryString()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Add",
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Delete",
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                    },
            },
            RouteData = new RouteData(),
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "Post",
                        QueryString = new QueryString("?handler=Delete"),
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Same(descriptor2, actual);
    }

    [Fact]
    public void Select_HandlerConsidersRouteDataFirst()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Add",
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Delete",
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                    },
            },
            RouteData = new RouteData
            {
                Values =
                    {
                        { "handler", "Add" }
                    }
            },
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "Post",
                        QueryString = new QueryString("?handler=Delete"),
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Same(descriptor1, actual);
    }

    [Fact]
    public void Select_HandlerMultipleTimesInQueryString_UsesFirst()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Add",
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Delete",
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                    },
            },
            RouteData = new RouteData(),
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "Post",
                        QueryString = new QueryString("?handler=Delete&handler=Add"),
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Same(descriptor2, actual);
    }

    [Fact]
    public void Select_ReturnsHandlerWithMatchingHttpMethodWithoutAHandler()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
            Name = "Subscribe",
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            HttpMethod = "POST",
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                    },
            },
            RouteData = new RouteData
            {
                Values =
                    {
                        { "handler", "Add" }
                    }
            },
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "Post"
                    },
            },
        };
        var selector = CreateSelector();

        // Act
        var actual = selector.Select(pageContext);

        // Assert
        Assert.Same(descriptor2, actual);
    }

    [Fact]
    public void Select_WithoutHandler_ThrowsIfMoreThanOneHandlerMatches()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            MethodInfo = GetType().GetMethod(nameof(Post)),
            HttpMethod = "POST",
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            MethodInfo = GetType().GetMethod(nameof(PostAsync)),
            HttpMethod = "POST",
        };

        var descriptor3 = new HandlerMethodDescriptor
        {
            HttpMethod = "GET",
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                        descriptor3,
                    },
            },
            RouteData = new RouteData(),
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "Post"
                    },
            },
        };
        var selector = CreateSelector();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => selector.Select(pageContext));
        var methods = descriptor1.MethodInfo + ", " + descriptor2.MethodInfo;
        var message = "Multiple handlers matched. The following handlers matched route data and had all constraints satisfied:" +
            Environment.NewLine + Environment.NewLine + methods;

        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Select_WithHandler_ThrowsIfMoreThanOneHandlerMatches()
    {
        // Arrange
        var descriptor1 = new HandlerMethodDescriptor
        {
            MethodInfo = GetType().GetMethod(nameof(Post)),
            HttpMethod = "POST",
            Name = "Add",
        };

        var descriptor2 = new HandlerMethodDescriptor
        {
            MethodInfo = GetType().GetMethod(nameof(PostAsync)),
            HttpMethod = "POST",
            Name = "Add",
        };

        var descriptor3 = new HandlerMethodDescriptor
        {
            HttpMethod = "GET",
        };

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerMethods = new List<HandlerMethodDescriptor>()
                    {
                        descriptor1,
                        descriptor2,
                        descriptor3,
                    },
            },
            RouteData = new RouteData
            {
                Values =
                    {
                        { "handler", "Add" }
                    }
            },
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Method = "Post"
                    },
            },
        };
        var selector = CreateSelector();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => selector.Select(pageContext));
        var methods = descriptor1.MethodInfo + ", " + descriptor2.MethodInfo;
        var message = "Multiple handlers matched. The following handlers matched route data and had all constraints satisfied:" +
            Environment.NewLine + Environment.NewLine + methods;

        Assert.Equal(message, ex.Message);
    }

    protected void Post()
    {
    }

    protected void PostAsync()
    {
    }

    private static DefaultPageHandlerMethodSelector CreateSelector()
    {
        return new DefaultPageHandlerMethodSelector();
    }
}
