// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;
using Xunit;

#if ASPNET50
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

#if ASPNET50
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
            var resultExecutingContext = CreateResultExecutingContext(
                format, 
                FormatSource.RouteData);            
            var resourceExecutingContext = CreateResourceExecutingContext(
                new IFilter[] { }, 
                format, 
                FormatSource.RouteData);
            var filter = new FormatFilterAttribute();

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

            var httpContext = CreateMockHttpContext();

            // Routedata contains json
            var data = new RouteData();
            data.Values.Add("format", "json");

            // Query contains xml
            httpContext.Setup(c => c.Request.Query.ContainsKey("format")).Returns(true);
            httpContext.Setup(c => c.Request.Query.Get("format")).Returns("xml");
            var ac = new ActionContext(httpContext.Object, data, new ActionDescriptor());

            var resultExecutingContext = new ResultExecutingContext(
                ac, 
                new IFilter[] { }, 
                new ObjectResult("Hello!"),
                controller: new object());
            
            var resourceExecutingContext = new ResourceExecutingContext(
                ac,
                new IFilter[] { });

            var filter = new FormatFilterAttribute();

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
            var resultExecutingContext = CreateResultExecutingContext(format, place);
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilter[] { }, format, place);
            var options = resultExecutingContext.HttpContext.RequestServices.GetService<IOptions<MvcOptions>>();
            options.Options.FormatterMappings.SetMediaTypeMappingForFormat(format, MediaTypeHeaderValue.Parse(contentType));
            
            var filter = new FormatFilterAttribute();

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
        public void FormatFilter_ContextContainsNonExistingFormat(
            string format,
            FormatSource place,
            string contentType)
        {
            // Arrange  
            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilter[] { }, format, place);          
            var filter = new FormatFilterAttribute();

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
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilter[] { });
            var filter = new FormatFilterAttribute();

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
            var context = CreateResourceExecutingContext(new IFilter[] { produces }, format, place);
            var filter = new FormatFilterAttribute();

            // Act
            filter.OnResourceExecuting(context);

            // Assert            
            Assert.Null(context.Result);
        }

        [Fact]
        public void FormatFilter_LessSpecificThan_Produces()
        {
            // Arrange
            var produces = new ProducesAttribute("application/xml;version=1", new string [] { });
            var context = CreateResourceExecutingContext(new IFilter[] { produces },  "xml", FormatSource.RouteData);
            var options = context.HttpContext.RequestServices.GetService<IOptions<MvcOptions>>();
            options.Options.FormatterMappings.SetMediaTypeMappingForFormat("xml", MediaTypeHeaderValue.Parse("application/xml"));
            var filter = new FormatFilterAttribute();

            // Act
            filter.OnResourceExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Fact]
        public void FormatFilter_MoreSpecificThan_Produces()
        {
            // Arrange
            var produces = new ProducesAttribute("application/xml", new string[] { });
            var context = CreateResourceExecutingContext(new IFilter[] { produces }, "xml", FormatSource.RouteData);
            var options = context.HttpContext.RequestServices.GetService<IOptions<MvcOptions>>();
            options.Options.FormatterMappings.SetMediaTypeMappingForFormat("xml", MediaTypeHeaderValue.Parse("application/xml;version=1"));
            var filter = new FormatFilterAttribute();

            // Act
            filter.OnResourceExecuting(context);

            // Assert            
            var actionResult = context.Result;
            Assert.IsType<HttpNotFoundResult>(actionResult);
        }

        [Theory]
        [InlineData("json", FormatSource.RouteData, "application/json")]
        [InlineData("json", FormatSource.QueryData, "application/json")]
        public void FormatFilter_ContextContainsFormat_ContainsProducesFilter_Conflicting(
            string format,
            FormatSource place,
            string contentType)
        {
            // Arrange
            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            var produces = new ProducesAttribute("application/xml", new string[] { "application/foo", "text/bar" });
            var context = CreateResourceExecutingContext(new IFilter[] { produces }, format, place);
            var filter = new FormatFilterAttribute();

            // Act
            filter.OnResourceExecuting(context);

            // Assert
            var result = Assert.IsType<HttpNotFoundResult>(context.Result);
        }

        [Theory]
        [InlineData("", FormatSource.RouteData)]
        [InlineData(null, FormatSource.QueryData)]
        [InlineData("", FormatSource.RouteData)]
        [InlineData(null, FormatSource.QueryData)]
        public void FormatFilter_ContextContainsFormat_Invalid(
            string format,
            FormatSource place)
        {
            // Arrange            
            var resourceExecutingContext = CreateResourceExecutingContext(
                new IFilter[] { }, 
                format, 
                place);
            var filter = new FormatFilterAttribute();

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            Assert.Null(resourceExecutingContext.Result);
        }

        [Theory]
        [InlineData("json", FormatSource.RouteData, true)]
        [InlineData("json", FormatSource.QueryData, true )]
        [InlineData("", FormatSource.RouteAndQueryData, false)]
        [InlineData(null, FormatSource.RouteAndQueryData, false)]
        public void FormatFilter_IsActive(
            string format,
            FormatSource place,
            bool expected)
        {
            // Arrange            
            var resultExecutingContext = CreateResultExecutingContext(format, place);
            var filter = new FormatFilterAttribute();

            // Act
            var isActive = filter.IsActive(resultExecutingContext);

            // Assert
            Assert.Equal(expected, isActive);
        }

        private static ResourceExecutingContext CreateResourceExecutingContext(
            IFilter[] filters,
            string format = null, 
            FormatSource? place = null)
        {
            if (format == null || place == null)
            {
                var context = new ResourceExecutingContext(
                    CreateActionContext(),
                    filters);
                return context;
            }

            var context1 = new ResourceExecutingContext(
                CreateActionContext(format, place),
                filters);
            return context1;
        }

        private static ResultExecutingContext CreateResultExecutingContext(
            string format = null,
            FormatSource? place = null)
        {
            if (format == null && place == null)
            {
                return new ResultExecutingContext(
                    new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                    new IFilter[] { },
                    new ObjectResult("Some Value"),
                    controller: new object());
            }

            return new ResultExecutingContext(
                CreateActionContext(format, place),
                new IFilter[] { },
                new ObjectResult("Some Value"),
                controller: new object());
        }

        private static ActionContext CreateActionContext(string format = null, FormatSource? place = null)
        {
            var httpContext = CreateMockHttpContext();
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

        private static Mock<HttpContext> CreateMockHttpContext()
        {
            var options = new MvcOptions();
            MvcOptionsSetup.ConfigureMvc(options);
            var mvcOptions = new Mock<IOptions<MvcOptions>>();
            mvcOptions.Setup(o => o.Options).Returns(options);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(IOptions<MvcOptions>))))
                .Returns(mvcOptions.Object);

            var httpContext = new Mock<HttpContext>();
            httpContext
                .Setup(c => c.RequestServices)
                .Returns(serviceProvider.Object);

            httpContext.Setup(c => c.Request.Query.ContainsKey("format")).Returns(false);
            return httpContext;
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
#endif
    }
}