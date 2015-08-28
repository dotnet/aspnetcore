// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;
using Xunit;

#if DNX451
using Moq;
using System.Net;
#endif

namespace Microsoft.AspNet.Mvc
{
    public class FormatFilterTests
    {
        public enum FormatSource
        {
            RouteData,
            QueryData,
            RouteAndQueryData
        }

#if DNX451
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
            var mediaType = MediaTypeHeaderValue.Parse("application/json");
            var mockObjects = new MockObjects(format, place);

            var resultExecutingContext = mockObjects.CreateResultExecutingContext();
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { });

            var filter = new FormatFilter(mockObjects.OptionsManager, mockObjects.ActionContextAccessor);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            Assert.Null(resourceExecutingContext.Result);

            // Act
            filter.OnResultExecuting(resultExecutingContext);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
            Assert.Equal(1, objectResult.ContentTypes.Count);
            AssertMediaTypesEqual(mediaType, objectResult.ContentTypes[0]);
        }

        [Fact]
        public void FormatFilter_ContextContainsFormat_InRouteAndQueryData()
        {
            // If the format is present in both route and query data, the one in route data wins

            // Arrange  
            var mediaType = MediaTypeHeaderValue.Parse("application/json");
            var mockObjects = new MockObjects("json", FormatSource.RouteData);
            var httpContext = new Mock<HttpContext>();

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
                new IFilterMetadata[] { });

            var filter = new FormatFilter(mockObjects.OptionsManager, mockObjects.ActionContextAccessor);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);
            filter.OnResultExecuting(resultExecutingContext);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
            Assert.Equal(1, objectResult.ContentTypes.Count);
            AssertMediaTypesEqual(mediaType, objectResult.ContentTypes[0]);
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
            var mediaType = MediaTypeHeaderValue.Parse(contentType);

            var mockObjects = new MockObjects(format, place);
            var resultExecutingContext = mockObjects.CreateResultExecutingContext();
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { });

            mockObjects.Options.FormatterMappings.SetMediaTypeMappingForFormat(
                format,
                MediaTypeHeaderValue.Parse(contentType));

            var filter = new FormatFilter(mockObjects.OptionsManager, mockObjects.ActionContextAccessor);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);
            filter.OnResultExecuting(resultExecutingContext);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
            Assert.Equal(1, objectResult.ContentTypes.Count);
            AssertMediaTypesEqual(mediaType, objectResult.ContentTypes[0]);
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

            var filter = new FormatFilter(mockObjects.OptionsManager, mockObjects.ActionContextAccessor);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            var actionResult = resourceExecutingContext.Result;
            Assert.IsType<HttpNotFoundResult>(actionResult);
        }

        [Fact]
        public void FormatFilter_ContextDoesntContainFormat()
        {
            // Arrange
            var mockObjects = new MockObjects();
            var resourceExecutingContext = mockObjects.CreateResourceExecutingContext(new IFilterMetadata[] { });

            var filter = new FormatFilter(mockObjects.OptionsManager, mockObjects.ActionContextAccessor);

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

            var filter = new FormatFilter(mockObjects.OptionsManager, mockObjects.ActionContextAccessor);

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

            mockObjects.Options.FormatterMappings.SetMediaTypeMappingForFormat(
                "xml",
                MediaTypeHeaderValue.Parse("application/xml"));

            var filter = new FormatFilter(mockObjects.OptionsManager, mockObjects.ActionContextAccessor);

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

            mockObjects.Options.FormatterMappings.SetMediaTypeMappingForFormat(
                "xml",
                MediaTypeHeaderValue.Parse("application/xml;version=1"));

            var filter = new FormatFilter(mockObjects.OptionsManager, mockObjects.ActionContextAccessor);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert            
            var actionResult = resourceExecutingContext.Result;
            Assert.IsType<HttpNotFoundResult>(actionResult);
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

            mockObjects.Options.FormatterMappings.SetMediaTypeMappingForFormat(
                "xml",
                MediaTypeHeaderValue.Parse("application/xml"));

            var filter = new FormatFilter(mockObjects.OptionsManager, mockObjects.ActionContextAccessor);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            var result = Assert.IsType<HttpNotFoundResult>(resourceExecutingContext.Result);
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
            var filter = new FormatFilter(mockObjects.OptionsManager, mockObjects.ActionContextAccessor);

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            Assert.Null(resourceExecutingContext.Result);
        }

        [Theory]
        [InlineData("json", FormatSource.RouteData, true)]
        [InlineData("json", FormatSource.QueryData, true)]
        [InlineData("", FormatSource.RouteAndQueryData, false)]
        [InlineData(null, FormatSource.RouteAndQueryData, false)]
        public void FormatFilter_IsActive(
            string format,
            FormatSource place,
            bool expected)
        {
            // Arrange
            var mockObjects = new MockObjects(format, place);
            var resultExecutingContext = mockObjects.CreateResultExecutingContext();
            var filterAttribute = new FormatFilterAttribute();
            var filter = new FormatFilter(mockObjects.OptionsManager, mockObjects.ActionContextAccessor);

            // Act and Assert
            Assert.Equal(expected, filter.IsActive);
        }

        private static void AssertMediaTypesEqual(
            MediaTypeHeaderValue expectedMediaType,
            MediaTypeHeaderValue actualMediaType)
        {
            Assert.Equal(expectedMediaType.MediaType, actualMediaType.MediaType);
            Assert.Equal(expectedMediaType.SubType, actualMediaType.SubType);
            Assert.Equal(expectedMediaType.Charset, actualMediaType.Charset);
            Assert.Equal(expectedMediaType.MatchesAllTypes, actualMediaType.MatchesAllTypes);
            Assert.Equal(expectedMediaType.MatchesAllSubTypes, actualMediaType.MatchesAllSubTypes);
            Assert.Equal(expectedMediaType.Parameters.Count, actualMediaType.Parameters.Count);
            foreach (var item in expectedMediaType.Parameters)
            {
                Assert.Equal(item.Value, NameValueHeaderValue.Find(actualMediaType.Parameters, item.Name).Value);
            }
        }

        private class MockObjects
        {
            public MvcOptions Options { get; private set; }
            public HttpContext MockHttpContext { get; private set; }
            public ActionContext MockActionContext { get; private set; }

            public IActionContextAccessor ActionContextAccessor { get; private set; }
            public IOptions<MvcOptions> OptionsManager { get; private set; }

            public MockObjects(string format = null, FormatSource? place = null)
            {
                var httpContext = new Mock<HttpContext>();
                httpContext.Setup(c => c.Request.Query.ContainsKey("format")).Returns(false);
                MockHttpContext = httpContext.Object;

                Initialize(httpContext, format, place);
            }

            public ResourceExecutingContext CreateResourceExecutingContext(IFilterMetadata[] filters)
            {
                var context = new ResourceExecutingContext(
                    MockActionContext,
                    filters);
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
                OptionsManager = new TestOptionsManager<MvcOptions>();

                // Setup options on mock service provider
                Options = OptionsManager.Options;

                // Set up default output formatters.
                Options.OutputFormatters.Add(new HttpNoContentOutputFormatter());
                Options.OutputFormatters.Add(new StringOutputFormatter());
                Options.OutputFormatters.Add(new JsonOutputFormatter());

                // Set up default mapping for json extensions to content type
                Options.FormatterMappings.SetMediaTypeMappingForFormat(
                    "json",
                    MediaTypeHeaderValue.Parse("application/json"));

                // Setup MVC services on mock service provider
                MockActionContext = CreateMockActionContext(httpContext, format, place);
                ActionContextAccessor = new ActionContextAccessor()
                {
                    ActionContext = MockActionContext,
                };
            }
        }
#endif
    }
}
