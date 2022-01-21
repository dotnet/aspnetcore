// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class ProducesAttributeTests
{
    [Fact]
    public void ProducesAttribute_SetsContentType()
    {
        // Arrange
        var mediaType1 = new StringSegment("application/json");
        var mediaType2 = new StringSegment("text/json;charset=utf-8");
        var producesContentAttribute = new ProducesAttribute("application/json", "text/json;charset=utf-8");
        var resultExecutingContext = CreateResultExecutingContext(new IFilterMetadata[] { producesContentAttribute });
        var next = new ResultExecutionDelegate(
                        () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

        // Act
        producesContentAttribute.OnResultExecuting(resultExecutingContext);

        // Assert
        var objectResult = resultExecutingContext.Result as ObjectResult;
        Assert.Equal(2, objectResult.ContentTypes.Count);
        MediaTypeAssert.Equal(mediaType1, objectResult.ContentTypes[0]);
        MediaTypeAssert.Equal(mediaType2, objectResult.ContentTypes[1]);
    }

    [Fact]
    public void ProducesContentAttribute_FormatFilterAttribute_NotActive()
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
        producesContentAttribute.OnResultExecuting(resultExecutingContext);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
        Assert.Single(objectResult.ContentTypes);
    }

    [Fact]
    public void ProducesContentAttribute_FormatFilterAttribute_Active()
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
        producesContentAttribute.OnResultExecuting(resultExecutingContext);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(resultExecutingContext.Result);
        Assert.Empty(objectResult.ContentTypes);
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
        Assert.Equal("The header contains invalid values at index 0: '" + (invalidContentType ?? "<null>") + "'",
                     ex.Message);
    }

    [Theory]
    [InlineData("application/*", "application/*")]
    [InlineData("application/xml, application/*, application/json", "application/*")]
    [InlineData("application/*, application/json", "application/*")]

    [InlineData("*/*", "*/*")]
    [InlineData("application/xml, */*, application/json", "*/*")]
    [InlineData("*/*, application/json", "*/*")]
    [InlineData("application/*+json", "application/*+json")]
    [InlineData("application/json;v=1;*", "application/json;v=1;*")]
    public void ProducesAttribute_InvalidContentType_Throws(string content, string invalidContentType)
    {
        // Act
        var contentTypes = content.Split(',').Select(contentType => contentType.Trim()).ToArray();

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(
                   () => new ProducesAttribute(contentTypes[0], contentTypes.Skip(1).ToArray()));

        Assert.Equal(
            $"The argument '{invalidContentType}' is invalid. " +
            "Media types which match all types or match all subtypes are not supported.",
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
