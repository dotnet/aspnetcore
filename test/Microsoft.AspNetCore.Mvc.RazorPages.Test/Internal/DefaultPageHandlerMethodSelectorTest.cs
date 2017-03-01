// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
                    HandlerMethods =
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
                    HandlerMethods =
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
                    HandlerMethods =
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
        public void Select_ReturnsNullWhenNoHandlerMatchesFormAction()
        {
            // Arrange
            var descriptor1 = new HandlerMethodDescriptor
            {
                HttpMethod = "POST",
                FormAction = new StringSegment("Add"),
            };

            var descriptor2 = new HandlerMethodDescriptor
            {
                HttpMethod = "POST",
                FormAction = new StringSegment("Delete"),
            };

            var pageContext = new PageContext
            {
                ActionDescriptor = new CompiledPageActionDescriptor
                {
                    HandlerMethods =
                    {
                        descriptor1,
                        descriptor2,
                    },
                },
                RouteData = new RouteData
                {
                    Values =
                    {
                        { "formaction", "update" }
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
        public void Select_ReturnsHandlerThatMatchesFormAction()
        {
            // Arrange
            var descriptor1 = new HandlerMethodDescriptor
            {
                HttpMethod = "POST",
                FormAction = new StringSegment("Add"),
            };

            var descriptor2 = new HandlerMethodDescriptor
            {
                HttpMethod = "POST",
                FormAction = new StringSegment("Delete"),
            };

            var pageContext = new PageContext
            {
                ActionDescriptor = new CompiledPageActionDescriptor
                {
                    HandlerMethods =
                    {
                        descriptor1,
                        descriptor2,
                    },
                },
                RouteData = new RouteData
                {
                    Values =
                    {
                        { "formaction", "Add" }
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
        public void Select_ReturnsHandlerWithMatchingHttpMethodWithoutAFormAction()
        {
            // Arrange
            var descriptor1 = new HandlerMethodDescriptor
            {
                HttpMethod = "POST",
                FormAction = new StringSegment("Subscribe"),
            };

            var descriptor2 = new HandlerMethodDescriptor
            {
                HttpMethod = "POST",
            };

            var pageContext = new PageContext
            {
                ActionDescriptor = new CompiledPageActionDescriptor
                {
                    HandlerMethods =
                    {
                        descriptor1,
                        descriptor2,
                    },
                },
                RouteData = new RouteData
                {
                    Values =
                    {
                        { "formaction", "Add" }
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
        public void Select_WithoutFormAction_ThrowsIfMoreThanOneHandlerMatches()
        {
            // Arrange
            var descriptor1 = new HandlerMethodDescriptor
            {
                Method = GetType().GetMethod(nameof(Post)),
                HttpMethod = "POST",
            };

            var descriptor2 = new HandlerMethodDescriptor
            {
                Method = GetType().GetMethod(nameof(PostAsync)),
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
                    HandlerMethods =
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
            var methods = descriptor1.Method + ", " + descriptor2.Method;
            var message = "Multiple handlers matched. The following handlers matched route data and had all constraints satisfied:" +
                Environment.NewLine + Environment.NewLine + methods;

            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void Select_WithFormAction_ThrowsIfMoreThanOneHandlerMatches()
        {
            // Arrange
            var descriptor1 = new HandlerMethodDescriptor
            {
                Method = GetType().GetMethod(nameof(Post)),
                HttpMethod = "POST",
                FormAction = new StringSegment("Add"),
            };

            var descriptor2 = new HandlerMethodDescriptor
            {
                Method = GetType().GetMethod(nameof(PostAsync)),
                HttpMethod = "POST",
                FormAction = new StringSegment("Add"),
            };

            var descriptor3 = new HandlerMethodDescriptor
            {
                HttpMethod = "GET",
            };

            var pageContext = new PageContext
            {
                ActionDescriptor = new CompiledPageActionDescriptor
                {
                    HandlerMethods =
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
                        { "formaction", "Add" }
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
            var methods = descriptor1.Method + ", " + descriptor2.Method;
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
