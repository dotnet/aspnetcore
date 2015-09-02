// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.WebApiCompatShim;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.OptionsModel;
#if !DNXCORE50
using Moq;
#endif
using Xunit;

namespace System.Net.Http
{
    public class HttpRequestMessageExtensionsTest
    {
        [Fact]
        public void CreateResponse_MatchingMediaType_WhenMediaTypeStringIsInvalidFormat_Throws()
        {
            HttpRequestMessage request = CreateRequest(new DefaultHttpContext());

            var ex = Assert.Throws<FormatException>(
                () => request.CreateResponse(HttpStatusCode.OK, CreateValue(), "foo/bar; param=value"));

            Assert.Equal(
                TestPlatformHelper.IsMono ?
                "Invalid format." :
                "The format of value 'foo/bar; param=value' is invalid.", ex.Message);
        }

        [Fact]
        public void CreateResponse_MatchingMediaType_WhenRequestDoesNotHaveHttpContextThrows()
        {
            HttpRequestMessage request = CreateRequest(null);

            // Arrange

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => request.CreateResponse(HttpStatusCode.OK, CreateValue(), mediaType: "foo/bar"));

            Assert.Equal(
                "The HttpRequestMessage instance is not properly initialized. " +
                "Use HttpRequestMessageHttpContextExtensions.GetHttpRequestMessage to create an HttpRequestMessage " +
                "for the current request.",
                ex.Message);
        }

#if !DNXCORE50

        [Fact]
        public void CreateResponse_DoingConneg_OnlyContent_RetrievesContentNegotiatorFromServices()
        {
            // Arrange
            var context = new DefaultHttpContext();

            var services = new Mock<IServiceProvider>();
            services
                .Setup(s => s.GetService(typeof(IContentNegotiator)))
                .Returns(Mock.Of<IContentNegotiator>())
                .Verifiable();

            var options = new WebApiCompatShimOptions();
            options.Formatters.AddRange(new MediaTypeFormatterCollection());

            var optionsAccessor = new Mock<IOptions<WebApiCompatShimOptions>>();
            optionsAccessor.SetupGet(o => o.Value).Returns(options);

            services
                .Setup(s => s.GetService(typeof(IOptions<WebApiCompatShimOptions>)))
                .Returns(optionsAccessor.Object);

            context.RequestServices = services.Object;

            var request = CreateRequest(context);

            // Act
            request.CreateResponse(CreateValue());

            // Assert
            services.Verify();
        }

        [Fact]
        public void CreateResponse_DoingConneg_RetrievesContentNegotiatorFromServices()
        {
            // Arrange
            var context = new DefaultHttpContext();

            var services = new Mock<IServiceProvider>();
            services
                .Setup(s => s.GetService(typeof(IContentNegotiator)))
                .Returns(Mock.Of<IContentNegotiator>())
                .Verifiable();

            var options = new WebApiCompatShimOptions();
            options.Formatters.AddRange(new MediaTypeFormatterCollection());

            var optionsAccessor = new Mock<IOptions<WebApiCompatShimOptions>>();
            optionsAccessor.SetupGet(o => o.Value).Returns(options);

            services
                .Setup(s => s.GetService(typeof(IOptions<WebApiCompatShimOptions>)))
                .Returns(optionsAccessor.Object);

            context.RequestServices = services.Object;

            var request = CreateRequest(context);

            // Act
            request.CreateResponse(HttpStatusCode.OK, CreateValue());

            // Assert
            services.Verify();
        }

        [Fact]
        public void CreateResponse_DoingConneg_PerformsContentNegotiationAndCreatesContentUsingResults()
        {
            // Arrange
            var context = new DefaultHttpContext();

            var formatter = new XmlMediaTypeFormatter();

            var contentNegotiator = new Mock<IContentNegotiator>();
            contentNegotiator
                .Setup(c => c.Negotiate(It.IsAny<Type>(), It.IsAny<HttpRequestMessage>(), It.IsAny<IEnumerable<MediaTypeFormatter>>()))
                .Returns(new ContentNegotiationResult(formatter, mediaType: null));

            context.RequestServices = CreateServices(contentNegotiator.Object, formatter);

            var request = CreateRequest(context);

            // Act
            var response = request.CreateResponse<string>(HttpStatusCode.NoContent, "42");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Same(request, response.RequestMessage);

            var objectContent = Assert.IsType<ObjectContent<string>>(response.Content);
            Assert.Equal("42", objectContent.Value);
            Assert.Same(formatter, objectContent.Formatter);
        }

        [Fact]
        public void CreateResponse_MatchingMediaType_WhenMediaTypeDoesNotMatch_Throws()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.RequestServices = CreateServices(new DefaultContentNegotiator());

            var request = CreateRequest(context);

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => request.CreateResponse(HttpStatusCode.OK, CreateValue(), mediaType: "foo/bar"));
            Assert.Equal(
                "Could not find a formatter matching the media type 'foo/bar' that can write an instance of 'System.Object'.",
                ex.Message);
        }

        [Fact]
        public void CreateResponse_MatchingMediaType_FindsMatchingFormatterAndCreatesResponse()
        {
            // Arrange
            var context = new DefaultHttpContext();

            var formatter = new Mock<MediaTypeFormatter> { CallBase = true };
            formatter.Setup(f => f.CanWriteType(typeof(object))).Returns(true).Verifiable();
            formatter.Object.SupportedMediaTypes.Add(new MediaTypeHeaderValue("foo/bar"));

            context.RequestServices = CreateServices(new DefaultContentNegotiator(), formatter.Object);

            var expectedValue = CreateValue();

            var request = CreateRequest(context);

            // Act
            var response = request.CreateResponse(HttpStatusCode.Gone, expectedValue, mediaType: "foo/bar");

            // Assert
            Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
            var content = Assert.IsType<ObjectContent<object>>(response.Content);
            Assert.Same(expectedValue, content.Value);
            Assert.Same(formatter.Object, content.Formatter);
            Assert.Equal("foo/bar", content.Headers.ContentType.MediaType);
            formatter.Verify();
        }

        [Fact]
        public void CreateResponse_AcceptingFormatter_CreatesResponseWithDefaultMediaType()
        {
            // Arrange
            var context = new DefaultHttpContext();

            var formatter = new Mock<MediaTypeFormatter>() { CallBase = true };
            formatter
                .Setup(f => f.CanWriteType(typeof(object)))
                .Returns(true)
                .Verifiable();
            formatter
                .Setup(f => f.SetDefaultContentHeaders(typeof(object), It.IsAny<HttpContentHeaders>(), It.IsAny<MediaTypeHeaderValue>()))
                .Callback<Type, HttpContentHeaders, MediaTypeHeaderValue>(SetMediaType)
                .Verifiable();

            formatter.Object.SupportedMediaTypes.Add(new MediaTypeHeaderValue("foo/bar"));

            var expectedValue = CreateValue();

            var request = CreateRequest(context);

            // Act
            var response = request.CreateResponse(
                HttpStatusCode.MultipleChoices,
                expectedValue,
                formatter.Object,
                mediaType: (string)null);

            // Assert
            Assert.Equal(HttpStatusCode.MultipleChoices, response.StatusCode);
            var content = Assert.IsType<ObjectContent<object>>(response.Content);
            Assert.Same(expectedValue, content.Value);
            Assert.Same(formatter.Object, content.Formatter);
            Assert.Equal("foo/bar", content.Headers.ContentType.MediaType);

            formatter.Verify();
        }

        private static void SetMediaType(Type type, HttpContentHeaders headers, MediaTypeHeaderValue value)
        {
            headers.ContentType = new MediaTypeHeaderValue("foo/bar");
        }

        [Fact]
        public void CreateResponse_AcceptingFormatter_WithOverridenMediaTypeString_CreatesResponse()
        {
            // Arrange
            var context = new DefaultHttpContext();

            var formatter = new Mock<MediaTypeFormatter> { CallBase = true };
            formatter.Setup(f => f.CanWriteType(typeof(object))).Returns(true).Verifiable();
            formatter.Object.SupportedMediaTypes.Add(new MediaTypeHeaderValue("foo/bar"));

            var expectedValue = CreateValue();

            var request = CreateRequest(context);

            // Act
            var response = request.CreateResponse(
                HttpStatusCode.MultipleChoices,
                CreateValue(),
                formatter.Object,
                mediaType: "bin/baz");

            // Assert
            Assert.Equal("bin/baz", response.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        public void CreateResponse_AcceptingFormatter_WithOverridenMediaTypeHeader_CreatesResponse()
        {
            // Arrange
            var context = new DefaultHttpContext();

            var formatter = new Mock<MediaTypeFormatter> { CallBase = true };
            formatter.Setup(f => f.CanWriteType(typeof(object))).Returns(true).Verifiable();
            formatter.Object.SupportedMediaTypes.Add(new MediaTypeHeaderValue("foo/bar"));

            var expectedValue = CreateValue();

            var request = CreateRequest(context);

            // Act
            var response = request.CreateResponse(
                HttpStatusCode.MultipleChoices,
                CreateValue(),
                formatter.Object,
                mediaType: new MediaTypeHeaderValue("bin/baz"));

            // Assert
            Assert.Equal("bin/baz", response.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        public void CreateErrorResponseRangeNotSatisfiable_SetsCorrectStatusCodeAndContentRangeHeader()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.RequestServices = CreateServices(new DefaultContentNegotiator());

            var request = CreateRequest(context);

            var expectedContentRange = new ContentRangeHeaderValue(length: 128);
            var invalidByteRangeException = new InvalidByteRangeException(expectedContentRange);

            // Act
            var response = request.CreateErrorResponse(invalidByteRangeException);

            // Assert
            Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, response.StatusCode);
            Assert.Same(expectedContentRange, response.Content.Headers.ContentRange);
        }

        private static IServiceProvider CreateServices(
            IContentNegotiator contentNegotiator = null,
            MediaTypeFormatter formatter = null)
        {
            var options = new WebApiCompatShimOptions();

            if (formatter == null)
            {
                options.Formatters.AddRange(new MediaTypeFormatterCollection());
            }
            else
            {
                options.Formatters.Add(formatter);
            }

            var optionsAccessor = new Mock<IOptions<WebApiCompatShimOptions>>();
            optionsAccessor.SetupGet(o => o.Value).Returns(options);

            var services = new Mock<IServiceProvider>(MockBehavior.Strict);
            services
                .Setup(s => s.GetService(typeof(IOptions<WebApiCompatShimOptions>)))
                .Returns(optionsAccessor.Object);

            if (contentNegotiator != null)
            {
                services
                    .Setup(s => s.GetService(typeof(IContentNegotiator)))
                    .Returns(contentNegotiator);
            }

            return services.Object;
        }
#endif
        private static object CreateValue()
        {
            return new object();
        }

        private static HttpRequestMessage CreateRequest(HttpContext context)
        {
            var request = new HttpRequestMessage();
            request.Properties.Add(nameof(HttpContext), context);
            return request;
        }
    }
}
