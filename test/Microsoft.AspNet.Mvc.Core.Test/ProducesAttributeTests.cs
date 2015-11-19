// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Routing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Test
{
    public class ProducesAttributeTests
    {
        [Fact]
        public async Task ProducesAttribute_SetsContentType()
        {
            // Arrange
            var mediaType1 = MediaTypeHeaderValue.Parse("application/json");
            var mediaType2 = MediaTypeHeaderValue.Parse("text/json;charset=utf-8");
            var producesContentAttribute = new ProducesAttribute("application/json", "text/json;charset=utf-8");
            var resultExecutingContext = CreateResultExecutingContext(new IFilterMetadata[] { producesContentAttribute });
            var next = new ResultExecutionDelegate(
                            () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            // Act
            await producesContentAttribute.OnResultExecutionAsync(resultExecutingContext, next);

            // Assert
            var objectResult = resultExecutingContext.Result as ObjectResult;
            Assert.Equal(2, objectResult.ContentTypes.Count);
            ValidateMediaType(mediaType1, objectResult.ContentTypes[0]);
            ValidateMediaType(mediaType2, objectResult.ContentTypes[1]);
        }

        [Fact]
        public async Task ProducesContentAttribute_FormatFilterAttribute_NotActive()
        {
            // Arrange
            var producesContentAttribute = new ProducesAttribute("application/xml");

            var formatFilter = new Mock<IFormatFilter>();
            formatFilter
                .Setup(f => f.GetFormat(It.IsAny<ActionContext>()))
                .Returns((string)null);

            var filters = new IFilterMetadata[] { producesContentAttribute, formatFilter.Object };
            var resultExecutingContext = CreateResultExecutingContext(filters);

            var next = new ResultExecutionDelegate(
                            () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            // Act
            await producesContentAttribute.OnResultExecutionAsync(resultExecutingContext, next);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
            Assert.Equal(1, objectResult.ContentTypes.Count);
        }

        [Fact]
        public async Task ProducesContentAttribute_FormatFilterAttribute_Active()
        {
            // Arrange
            var producesContentAttribute = new ProducesAttribute("application/xml");

            var formatFilter = new Mock<IFormatFilter>();
            formatFilter
                .Setup(f => f.GetFormat(It.IsAny<ActionContext>()))
                .Returns("xml");

            var filters = new IFilterMetadata[] { producesContentAttribute, formatFilter.Object };
            var resultExecutingContext = CreateResultExecutingContext(filters);

            var next = new ResultExecutionDelegate(
                            () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            // Act
            await producesContentAttribute.OnResultExecutionAsync(resultExecutingContext, next);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
            Assert.Equal(0, objectResult.ContentTypes.Count);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("application/xml,, application/json", "")]
        [InlineData(", application/json", "")]
        [InlineData("invalid", "invalid")]
        [InlineData("application/xml,invalid, application/json", "invalid")]
        [InlineData("invalid, application/json", "invalid")]
        public void ProducesAttribute_UnParsableContentType_Throws(string content, string invalidContentType)
        {
            // Act
            var contentTypes = content.Split(',').Select(contentType => contentType.Trim()).ToArray();

            // Assert
            var ex = Assert.Throws<FormatException>(
                       () => new ProducesAttribute(contentTypes[0], contentTypes.Skip(1).ToArray()));
            Assert.Equal("Invalid value '" + (invalidContentType ?? "<null>") + "'.", ex.Message);
        }

        [Theory]
        [InlineData("application/*", "application/*")]
        [InlineData("application/xml, application/*, application/json", "application/*")]
        [InlineData("application/*, application/json", "application/*")]

        [InlineData("*/*", "*/*")]
        [InlineData("application/xml, */*, application/json", "*/*")]
        [InlineData("*/*, application/json", "*/*")]
        public void ProducesAttribute_InvalidContentType_Throws(string content, string invalidContentType)
        {
            // Act
            var contentTypes = content.Split(',').Select(contentType => contentType.Trim()).ToArray();

            // Assert
            var ex = Assert.Throws<InvalidOperationException>(
                       () => new ProducesAttribute(contentTypes[0], contentTypes.Skip(1).ToArray()));

            Assert.Equal(
                string.Format("The argument '{0}' is invalid. "+
                              "Media types which match all types or match all subtypes are not supported.",
                              invalidContentType),
                ex.Message);
        }

        [Fact]
        public void ProducesAttribute_WithTypeOnly_SetsTypeProperty()
        {
            // Arrange
            var personType = typeof(Person);
            var producesAttribute = new ProducesAttribute(personType);

            // Act and Assert
            Assert.NotNull(producesAttribute.Type);
            Assert.Same(personType, producesAttribute.Type);
        }

        [Fact]
        public void ProducesAttribute_WithTypeOnly_DoesNotSetContentTypes()
        {
            // Arrange
            var producesAttribute = new ProducesAttribute(typeof(Person));

            // Act and Assert
            Assert.NotNull(producesAttribute.ContentTypes);
            Assert.Empty(producesAttribute.ContentTypes);
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

        private static ResultExecutedContext CreateResultExecutedContext(ResultExecutingContext context)
        {
            return new ResultExecutedContext(context, context.Filters, context.Result, context.Controller);
        }

        private static ResultExecutingContext CreateResultExecutingContext(IFilterMetadata[] filters)
        {
            return new ResultExecutingContext(
                CreateActionContext(),
                filters,
                new ObjectResult("Some Value"),
                controller: new object());
        }

        private static ActionContext CreateActionContext()
        {
            return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        }

        private class Person
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}
