// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class FormatFilterTests
    {
        public enum FormatSource
        {
            RouteData,
            QueryData,
            RouteAndQueryData
        }

        [Theory]
        [InlineData("json", FormatSource.RouteData, "application/json")]
        [InlineData("json", FormatSource.QueryData, "application/json")]
        [InlineData("json", FormatSource.RouteAndQueryData, "application/json")]
        public void FormatFilter_ContextContainsFormat_DefaultFormat(
            string format,
            FormatSource place,
            string contentType)
        {
            // Arrange
            var mediaType = new StringSegment("application/json");
            var mockObjects = new MockObjects(format, place);

            var resultExecutingContext = mockObjects.CreateResultExecutingContext();
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { });

            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            Assert.Null(resourceExecutingContext.Result);

            // Act
            filter.OnResultExecuting(resultExecutingContext);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
            Assert.Single(objectResult.ContentTypes);
            MediaTypeAssert.Equal(mediaType, objectResult.ContentTypes[0]);
        }

        [Fact]
        public void FormatFilter_ContextContainsFormat_InRouteAndQueryData()
        {
            // If the format is present in both route and query data, the one in route data wins

            // Arrange
            var mediaType = new StringSegment("application/json");
            var mockObjects = new MockObjects("json", FormatSource.RouteData);
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Response).Returns(new Mock<HttpResponse>().Object);

            // Query contains xml
            httpContext.Setup(c => c.Request.Query.ContainsKey("format")).Returns(true);
            httpContext.Setup(c => c.Request.Query["format"]).Returns("xml");

            // Routedata contains json
            var data = new RouteData();
            data.Values.Add("format", "json");

            var ac = new ActionContext(httpContext.Object, data, new ActionDescriptor());

            var resultExecutingContext = new ResultExecutingContext(
                ac,
                new IFilterMetadata[] { },
                new ObjectResult("Hello!"),
                controller: new object());

            var resourceExecutingContext = new ResourceExecutingContext(
                ac,
                new IFilterMetadata[] { },
                new List<IValueProviderFactory>());

            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);
            filter.OnResultExecuting(resultExecutingContext);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
            Assert.Single(objectResult.ContentTypes);
            MediaTypeAssert.Equal(mediaType, objectResult.ContentTypes[0]);
        }

        [Theory]
        [InlineData("foo", FormatSource.RouteData, "application/foo")]
        [InlineData("foo", FormatSource.QueryData, "application/foo")]
        [InlineData("foo", FormatSource.RouteAndQueryData, "application/foo")]
        public void FormatFilter_ContextContainsFormat_Custom(
            string format,
            FormatSource place,
            string contentType)
        {
            // Arrange
            var mediaType = new StringSegment(contentType);

            var mockObjects = new MockObjects(format, place);
            var resultExecutingContext = mockObjects.CreateResultExecutingContext();
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { });

            mockObjects.MvcOptions.FormatterMappings.SetMediaTypeMappingForFormat(
                format,
                MediaTypeHeaderValue.Parse(contentType));

            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);
            filter.OnResultExecuting(resultExecutingContext);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
            Assert.Single(objectResult.ContentTypes);
            MediaTypeAssert.Equal(mediaType, objectResult.ContentTypes[0]);
        }

        [Theory]
        [InlineData("foo", FormatSource.RouteData)]
        [InlineData("foo", FormatSource.QueryData)]
        public void FormatFilter_ContextContainsNonExistingFormat(
            string format,
            FormatSource place)
        {
            // Arrange
            var mockObjects = new MockObjects(format, place);
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { });

            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            var actionResult = resourceExecutingContext.Result;
            Assert.IsType<NotFoundResult>(actionResult);
        }

        [Fact]
        public void FormatFilter_ContextDoesntContainFormat()
        {
            // Arrange
            var mockObjects = new MockObjects();
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { });

            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            Assert.Null(resourceExecutingContext.Result);
        }

        [Theory]
        [InlineData("json", FormatSource.RouteData, "application/json")]
        [InlineData("json", FormatSource.QueryData, "application/json")]
        public void FormatFilter_ContextContainsFormat_ContainsProducesFilter_Matching(
            string format,
            FormatSource place,
            string contentType)
        {
            // Arrange
            var produces = new ProducesAttribute(contentType, new string[] { "application/foo", "text/bar" });
            var mockObjects = new MockObjects(format, place);
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { produces });

            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            Assert.Null(resourceExecutingContext.Result);
        }

        [Fact]
        public void FormatFilter_LessSpecificThan_Produces()
        {
            // Arrange
            var produces = new ProducesAttribute("application/xml;version=1", new string[] { });
            var mockObjects = new MockObjects("xml", FormatSource.RouteData);
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { produces });

            mockObjects.MvcOptions.FormatterMappings.SetMediaTypeMappingForFormat(
                "xml",
                MediaTypeHeaderValue.Parse("application/xml"));

            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            Assert.Null(resourceExecutingContext.Result);
        }

        [Fact]
        public void FormatFilter_MoreSpecificThan_Produces()
        {
            // Arrange
            var produces = new ProducesAttribute("application/xml", new string[] { });
            var mockObjects = new MockObjects("xml", FormatSource.RouteData);
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { produces });

            mockObjects.MvcOptions.FormatterMappings.SetMediaTypeMappingForFormat(
                "xml",
                MediaTypeHeaderValue.Parse("application/xml;version=1"));

            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            var actionResult = resourceExecutingContext.Result;
            Assert.IsType<NotFoundResult>(actionResult);
        }

        [Theory]
        [InlineData("json", FormatSource.RouteData)]
        [InlineData("json", FormatSource.QueryData)]
        public void FormatFilter_ContextContainsFormat_ContainsProducesFilter_Conflicting(
            string format,
            FormatSource place)
        {
            // Arrange
            var produces = new ProducesAttribute("application/xml", new string[] { "application/foo", "text/bar" });
            var mockObjects = new MockObjects(format, place);
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { produces });

            mockObjects.MvcOptions.FormatterMappings.SetMediaTypeMappingForFormat(
                "xml",
                MediaTypeHeaderValue.Parse("application/xml"));

            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            var result = Assert.IsType<NotFoundResult>(resourceExecutingContext.Result);
        }

        [Theory]
        [InlineData("", FormatSource.RouteData)]
        [InlineData(null, FormatSource.QueryData)]
        public void FormatFilter_ContextContainsFormat_Invalid(
            string format,
            FormatSource place)
        {
            // Arrange
            var mockObjects = new MockObjects(format, place);
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { });
            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            Assert.Null(resourceExecutingContext.Result);
        }

        [Theory]
        [InlineData("json", FormatSource.RouteData, "json")]
        [InlineData("json", FormatSource.QueryData, "json")]
        [InlineData("", FormatSource.RouteAndQueryData, null)]
        [InlineData(null, FormatSource.RouteAndQueryData, null)]
        public void FormatFilter_GetFormat(
            string input,
            FormatSource place,
            string expected)
        {
            // Arrange
            var mockObjects = new MockObjects(input, place);
            var context = mockObjects.CreateResultExecutingContext();
            var filterAttribute = new FormatFilterAttribute();
            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            var format = filter.GetFormat(context);

            // Assert
            Assert.Equal(expected, filter.GetFormat(context));
        }

        [Fact]
        public void FormatFilter_ExplicitContentType_SetOnObjectResult_TakesPrecedence()
        {
            // Arrange
            var mediaType = new StringSegment("application/foo");
            var mockObjects = new MockObjects("json", FormatSource.QueryData);
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Response).Returns(new Mock<HttpResponse>().Object);
            httpContext.Setup(c => c.Request.Query["format"]).Returns("json");
            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            var objectResult = new ObjectResult("Hello!");
            objectResult.ContentTypes.Add(new MediaTypeHeaderValue("application/foo"));
            var resultExecutingContext = new ResultExecutingContext(
                actionContext,
                new IFilterMetadata[] { },
                objectResult,
                controller: new object());

            var resourceExecutingContext = new ResourceExecutingContext(
                actionContext,
                new IFilterMetadata[] { },
                new List<IValueProviderFactory>());

            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);
            filter.OnResultExecuting(resultExecutingContext);

            // Assert
            var result = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
            Assert.Single(result.ContentTypes);
            MediaTypeAssert.Equal(mediaType, result.ContentTypes[0]);
        }

        [Fact]
        public void FormatFilter_ExplicitContentType_SetOnResponse_TakesPrecedence()
        {
            // Arrange
            var mediaType = MediaTypeHeaderValue.Parse("application/foo");
            var mockObjects = new MockObjects("json", FormatSource.QueryData);
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.ContentType).Returns("application/foo");
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Response).Returns(response.Object);
            httpContext.Setup(c => c.Request.Query["format"]).Returns("json");
            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            var resultExecutingContext = new ResultExecutingContext(
                actionContext,
                new IFilterMetadata[] { },
                new ObjectResult("Hello!"),
                controller: new object());

            var resourceExecutingContext = new ResourceExecutingContext(
                actionContext,
                new IFilterMetadata[] { },
                new List<IValueProviderFactory>());

            var filter = new FormatFilter(mockObjects.OptionsManager);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);
            filter.OnResultExecuting(resultExecutingContext);

            // Assert
            var result = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
            Assert.Empty(result.ContentTypes);
        }

        private class MockObjects
        {
            public MvcOptions MvcOptions { get; private set; }
            public HttpContext MockHttpContext { get; private set; }
            public ActionContext MockActionContext { get; private set; }

            public IOptions<MvcOptions> OptionsManager { get; private set; }

            public MockObjects(string format = null, FormatSource? place = null)
            {
                var httpContext = new Mock<HttpContext>();
                httpContext.Setup(c => c.Request.Query.ContainsKey("format")).Returns(false);
                httpContext.Setup(c => c.Response).Returns(new Mock<HttpResponse>().Object);
                MockHttpContext = httpContext.Object;

                Initialize(httpContext, format, place);
            }

            public ResourceExecutingContext CreateResourceExecutingContext(IFilterMetadata[] filters)
            {
                var context = new ResourceExecutingContext(
                    MockActionContext,
                    filters,
                    new List<IValueProviderFactory>());
                return context;
            }

            public ResultExecutingContext CreateResultExecutingContext()
            {
                return new ResultExecutingContext(
                    MockActionContext,
                    new IFilterMetadata[] { },
                    new ObjectResult("Some Value"),
                    controller: new object());
            }

            private ActionContext CreateMockActionContext(
                Mock<HttpContext> httpContext,
                string format,
                FormatSource? place)
            {
                var data = new RouteData();

                if (place == FormatSource.RouteData || place == FormatSource.RouteAndQueryData)
                {
                    data.Values.Add("format", format);
                    httpContext.Setup(c => c.Request.Query.ContainsKey("format")).Returns(false);
                }

                if (place == FormatSource.QueryData || place == FormatSource.RouteAndQueryData)
                {
                    httpContext.Setup(c => c.Request.Query.ContainsKey("format")).Returns(true);
                    httpContext.Setup(c => c.Request.Query["format"]).Returns(format);
                }
                else if (place == null && format == null)
                {
                    httpContext.Setup(c => c.Request.Query.ContainsKey("format")).Returns(false);
                }

                return new ActionContext(httpContext.Object, data, new ActionDescriptor());
            }

            private void Initialize(
                Mock<HttpContext> httpContext,
                string format = null,
                FormatSource? place = null)
            {
                OptionsManager = Options.Create(new MvcOptions());

                // Setup options on mock service provider
                MvcOptions = OptionsManager.Value;

                // Set up default output formatters.
                MvcOptions.OutputFormatters.Add(new HttpNoContentOutputFormatter());
                MvcOptions.OutputFormatters.Add(new StringOutputFormatter());
                MvcOptions.OutputFormatters.Add(new JsonOutputFormatter(
                    new JsonSerializerSettings(),
                    ArrayPool<char>.Shared));

                // Set up default mapping for json extensions to content type
                MvcOptions.FormatterMappings.SetMediaTypeMappingForFormat(
                    "json",
                    MediaTypeHeaderValue.Parse("application/json"));

                // Setup MVC services on mock service provider
                MockActionContext = CreateMockActionContext(httpContext, format, place);
            }
        }
    }
}
