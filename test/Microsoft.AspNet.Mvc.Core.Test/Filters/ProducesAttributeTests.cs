// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Test
{
    public class ProducesAttributeTests
    {
        [Fact]
        public async Task ProducesContentAttribute_SetsContentType()
        {
            // Arrange
            var mediaType1 = MediaTypeHeaderValue.Parse("application/json");
            var mediaType2 = MediaTypeHeaderValue.Parse("text/json;charset=utf-8");
            var producesContentAttribute = new ProducesAttribute("application/json", "text/json;charset=utf-8");
            var resultExecutingContext = CreateResultExecutingContext(producesContentAttribute);
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

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("invalid")]
        public void ProducesAttribute_InvalidContentType_Throws(string content)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(
                       () => new ProducesAttribute(content));
            Assert.Equal("Invalid Argument. Content type '" + content + "' could not be parsed.",
                         ex.Message);
        }

        private static void ValidateMediaType(MediaTypeHeaderValue expectedMediaType, MediaTypeHeaderValue actualMediaType)
        {
            Assert.Equal(expectedMediaType.MediaType, actualMediaType.MediaType);
            Assert.Equal(expectedMediaType.MediaSubType, actualMediaType.MediaSubType);
            Assert.Equal(expectedMediaType.Charset, actualMediaType.Charset);
            Assert.Equal(expectedMediaType.MediaTypeRange, actualMediaType.MediaTypeRange);
            Assert.Equal(expectedMediaType.Parameters.Count, actualMediaType.Parameters.Count);
            foreach (var item in expectedMediaType.Parameters)
            {
                Assert.Equal(item.Value, actualMediaType.Parameters[item.Key]);
            }
        }

        private static ResultExecutedContext CreateResultExecutedContext(ResultExecutingContext context)
        {
            return new ResultExecutedContext(context, context.Filters, context.Result);
        }

        private static ResultExecutingContext CreateResultExecutingContext(IFilter filter)
        {
            return new ResultExecutingContext(
                CreateActionContext(),
                new IFilter[] { filter, },
                new ObjectResult("Some Value"));
        }

        private static ActionContext CreateActionContext()
        {
            return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        }
    }
}