using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core.Filters;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Microsoft.Net.Http.Headers;
using Xunit;

#if ASPNET50
using Moq;
using Microsoft.Framework.OptionsModel;
using Microsoft.AspNet.Http;
#endif

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public enum FormatPlace
    {
        RouteData,
        QueryData,
        RouteAndQueryData
    }

    public class FormatFilterTests
    {
        [Theory]
        [InlineData("json", FormatPlace.RouteData, "application/json")]
        [InlineData("json", FormatPlace.QueryData, "application/json")]
        [InlineData("json", FormatPlace.RouteAndQueryData, "application/json")]
        public void FormatFilter_ContextContainsFormat_DefaultFormat(string format, 
            FormatPlace place, 
            string contentType)
        {
            // Arrange  
            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            var context = CreateResultExecutingContext(format, place);
            var filter = new FormatFilter();

            // Act
            filter.OnResultExecuting(context);
            
            // Assert
            var objectResult = context.Result as ObjectResult;
            Assert.Equal(1, objectResult.ContentTypes.Count);
            ValidateMediaType(mediaType, objectResult.ContentTypes[0]);
        }

        [Theory]
        [InlineData("foo", FormatPlace.RouteData, "application/foo")]
        [InlineData("foo", FormatPlace.QueryData, "application/foo")]
        [InlineData("foo", FormatPlace.RouteAndQueryData, "application/foo")]
        public void FormatFilter_ContextContainsFormat_Custom(
            string format, 
            FormatPlace place, 
            string contentType)
        {
            // Arrange  
            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            var context = CreateResultExecutingContext(format, place);
            var options = (IOptions<MvcOptions>)context.HttpContext.RequestServices.GetService(
                                                                                    typeof(IOptions<MvcOptions>));
            options.Options.AddFormatMapping(format, MediaTypeHeaderValue.Parse(contentType));
            var filter = new FormatFilter();

            // Act
            filter.OnResultExecuting(context);

            // Assert
            var objectResult = context.Result as ObjectResult;
            Assert.Equal(1, objectResult.ContentTypes.Count);
            ValidateMediaType(mediaType, objectResult.ContentTypes[0]);
        }

        [Theory]
        [InlineData("foo", FormatPlace.RouteData, "application/foo")]
        public void FormatFilter_ContextContainsFormat_NonExisting(
            string format,
            FormatPlace place,
            string contentType)
        {
            // Arrange  
            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilter[] { }, format, place);          
            var filter = new FormatFilter();

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            var actionResult = resourceExecutingContext.Result;
            Assert.True(actionResult is HttpNotFoundResult);
        }

        [Fact]
        public void FormatFilter_ContextDoesntContainFormat()
        {
            // Arrange              
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilter[] { });
            var filter = new FormatFilter();

            // Act
            filter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            var result = resourceExecutingContext.Result as IActionResult;
            Assert.False(result is HttpNotFoundResult);
        }

        [Theory]
        [InlineData("json", FormatPlace.RouteData, "application/json")]
        [InlineData("json", FormatPlace.QueryData, "application/json")]
        public void FormatFilter_ContextContainsFormat_ContainsProducesFilter_Matching(
            string format, 
            FormatPlace place, 
            string contentType)
        {
            // Arrange
            var produces = new ProducesAttribute(contentType, new string[] { "application/foo", "text/bar" });
            var context = CreateResourceExecutingContext(new IFilter[] { produces }, format, place);
            var filter = new FormatFilter();

            // Act
            filter.OnResourceExecuting(context);

            // Assert
            var result = context.Result as IActionResult;
            Assert.False(result is HttpNotFoundResult);
        }

        [Theory]
        [InlineData("json", FormatPlace.RouteData, "application/json")]
        [InlineData("json", FormatPlace.QueryData, "application/json")]
        public void FormatFilter_ContextContainsFormat_ContainsProducesFilter_Conflicting(
            string format,
            FormatPlace place,
            string contentType)
        {
            // Arrange
            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            var produces = new ProducesAttribute("application/xml", new string[] { "application/foo", "text/bar" });
            var context = CreateResourceExecutingContext(new IFilter[] { produces }, format, place);
            var filter = new FormatFilter();

            // Act
            filter.OnResourceExecuting(context);

            // Assert
            var result = context.Result as IActionResult;
            Assert.True(result is HttpNotFoundResult);
        }

        private static ResourceExecutingContext CreateResourceExecutingContext(
            IFilter[] filters,
            string format = null, 
            FormatPlace? place = null)
        {
            if(format == null || place == null)
            {
                var context = new ResourceExecutingContext(
                    CreateActionContext(),
                    filters);
                context.Result = new HttpStatusCodeResult(200);
                return context;
            }

            var context1 = new ResourceExecutingContext(
                CreateActionContext(format, place),
                filters);
            context1.Result = new HttpStatusCodeResult(200);
            return context1;
        }

        private static ResultExecutingContext CreateResultExecutingContext(
            string format = null,
            FormatPlace? place = null)
        {
            if (format == null || place == null)
            {
                return new ResultExecutingContext(
                    new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                    new IFilter[] { },
                    new ObjectResult("Some Value"));
            }

            return new ResultExecutingContext(
                CreateActionContext(format, place),
                new IFilter[] { },
                new ObjectResult("Some Value"));
        }

        private static ActionContext CreateActionContext(string format = null, FormatPlace? place = null)
        {
            var httpContext = CreateMockHttpContext();

            if (place == FormatPlace.RouteData || place == FormatPlace.RouteAndQueryData)
            {
                var data = new RouteData();
                data.Values.Add("format", format);
                httpContext.Setup(c => c.Request.Query.ContainsKey("format")).Returns(false);
                return new ActionContext(httpContext.Object, data, new ActionDescriptor());
            }

            if (place == FormatPlace.QueryData || place == FormatPlace.RouteAndQueryData)
            {
                httpContext.Setup(c => c.Request.Query.ContainsKey("format")).Returns(true);
                httpContext.Setup(c => c.Request.Query.Get("format")).Returns(format);
                return new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            }
            else if(place == null && format == null)
            {
                httpContext.Setup(c => c.Request.Query.ContainsKey("format")).Returns(false);
                return new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            }

            return null;
        }

        private static Mock<HttpContext> CreateMockHttpContext()
        {
            MvcOptions options = new MvcOptions();
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

        private static void ValidateMediaType(MediaTypeHeaderValue expectedMediaType, MediaTypeHeaderValue actualMediaType)
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
    }
}