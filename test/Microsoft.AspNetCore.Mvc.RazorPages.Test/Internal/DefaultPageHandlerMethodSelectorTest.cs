// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class DefaultPageHandlerMethodSelectorTest
    {
        [Fact]
        public void Select_ReturnsNull_WhenNoHandlerMatchesHttpMethod()
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
                        Method = "PUT"
                    },
                },
            };
            var selector = new DefaultPageHandlerMethodSelector();

            // Act
            var actual = selector.Select(pageContext);

            // Assert
            Assert.Null(actual);
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
            var selector = new DefaultPageHandlerMethodSelector();

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
            var selector = new DefaultPageHandlerMethodSelector();

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
            var selector = new DefaultPageHandlerMethodSelector();

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
            var selector = new DefaultPageHandlerMethodSelector();

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
            var selector = new DefaultPageHandlerMethodSelector();

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
            var selector = new DefaultPageHandlerMethodSelector();

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
            var selector = new DefaultPageHandlerMethodSelector();

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
            var selector = new DefaultPageHandlerMethodSelector();

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
            var selector = new DefaultPageHandlerMethodSelector();

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
            var selector = new DefaultPageHandlerMethodSelector();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => selector.Select(pageContext));
            var methods = descriptor1.MethodInfo + ", " + descriptor2.MethodInfo;
            var message = "Multiple handlers matched. The following handlers matched route data and had all constraints satisfied:" +
                Environment.NewLine + Environment.NewLine + methods;

            Assert.Equal(message, ex.Message);
        }

        public void Post()
        {
        }

        public void PostAsync()
        {
        }
    }
}
