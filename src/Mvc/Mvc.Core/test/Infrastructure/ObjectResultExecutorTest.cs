// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class ObjectResultExecutorTest
{
    [Fact]
    public async Task ExecuteAsync_UsesSpecifiedContentType()
    {
        // Arrange
        var executor = CreateExecutor();

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext() { HttpContext = httpContext };
        httpContext.Request.Headers.Accept = "application/xml"; // This will not be used
        httpContext.Response.ContentType = "text/json";

        var result = new ObjectResult("input")
        {
            ContentTypes = { "text/xml", },
        };
        result.Formatters.Add(new TestXmlOutputFormatter());
        result.Formatters.Add(new TestJsonOutputFormatter());
        result.Formatters.Add(new TestStringOutputFormatter()); // This will be chosen based on the content type

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        MediaTypeAssert.Equal("text/xml; charset=utf-8", httpContext.Response.ContentType);
    }

    // For this test case probably the most common use case is when there is a format mapping based
    // content type selected but the developer had set the content type on the Response.ContentType
    [Fact]
    public async Task ExecuteAsync_ContentTypeProvidedFromResponseAndObjectResult_UsesResponseContentType()
    {
        // Arrange
        var executor = CreateExecutor();

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext() { HttpContext = httpContext };
        httpContext.Request.Headers.Accept = "application/xml"; // This will not be used
        httpContext.Response.ContentType = "text/plain";

        var result = new ObjectResult("input");
        result.Formatters.Add(new TestXmlOutputFormatter());
        result.Formatters.Add(new TestJsonOutputFormatter());
        result.Formatters.Add(new TestStringOutputFormatter()); // This will be chosen based on the content type

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        MediaTypeAssert.Equal("text/plain; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_WithOneProvidedContentType_FromResponseContentType_IgnoresAcceptHeader()
    {
        // Arrange
        var executor = CreateExecutor();

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext() { HttpContext = httpContext };
        httpContext.Request.Headers.Accept = "application/xml"; // This will not be used
        httpContext.Response.ContentType = "application/json";

        var result = new ObjectResult("input");
        result.Formatters.Add(new TestXmlOutputFormatter());
        result.Formatters.Add(new TestJsonOutputFormatter()); // This will be chosen based on the content type

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_WithOneProvidedContentType_FromResponseContentType_NoFallback()
    {
        // Arrange
        var executor = CreateExecutor();

        var httpContext = GetHttpContext();
        var actionContext = new ActionContext() { HttpContext = httpContext };
        httpContext.Request.Headers.Accept = "application/xml"; // This will not be used
        httpContext.Response.ContentType = "application/json";

        var result = new ObjectResult("input");
        result.Formatters.Add(new TestXmlOutputFormatter());

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        Assert.Equal(406, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ExecuteAsync_WithResponseAndObjectResultContentType_ForProblemDetailsValue_UsesXMLContentType()
    {
        // Arrange
        var executor = CreateExecutor();

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext() { HttpContext = httpContext };
        httpContext.Response.ContentType = "application/xml"; // This will not be used

        var result = new ObjectResult(new ProblemDetails())
        {
            ContentTypes = { "text/plain" }, // This will not be used
        };
        result.Formatters.Add(new TestXmlOutputFormatter());  // This will be chosen based on the problem details content type
        result.Formatters.Add(new TestJsonOutputFormatter());
        result.Formatters.Add(new TestStringOutputFormatter());

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        MediaTypeAssert.Equal("application/problem+xml; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_WithResponseContentType_ForProblemDetailsValue_UsesProblemDetailXMLContentType()
    {
        // Arrange
        var executor = CreateExecutor();

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext() { HttpContext = httpContext };
        httpContext.Response.ContentType = "application/json"; // This will not be used

        var result = new ObjectResult(new ProblemDetails());
        result.Formatters.Add(new TestXmlOutputFormatter()); // This will be chosen based on the problem details content type
        result.Formatters.Add(new TestJsonOutputFormatter());
        result.Formatters.Add(new TestStringOutputFormatter());

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        MediaTypeAssert.Equal("application/problem+xml; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_ForProblemDetailsValue_UsesProblemDetailsContentType()
    {
        // Arrange
        var executor = CreateExecutor();

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext() { HttpContext = httpContext };
        httpContext.Response.ContentType = "application/json"; // This will not be used

        var result = new ObjectResult(new ProblemDetails());
        result.Formatters.Add(new TestXmlOutputFormatter()); // This will be chosen based on the problem details content type
        result.Formatters.Add(new TestJsonOutputFormatter());
        result.Formatters.Add(new TestStringOutputFormatter());

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        MediaTypeAssert.Equal("application/problem+xml; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_ForProblemDetailsValue_UsesProblemDetailsJsonContentType_BasedOnAcceptHeader()
    {
        // Arrange
        var executor = CreateExecutor();

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext() { HttpContext = httpContext };
        httpContext.Request.Headers.Accept = "application/json"; // This will not be used
        httpContext.Response.ContentType = "application/xml"; // This will not be used

        var result = new ObjectResult(new ProblemDetails())
        {
            ContentTypes = { "text/plain" }, // This will not be used
        };
        result.Formatters.Add(new TestJsonOutputFormatter()); // This will be chosen based on the Accept Headers "application/json"
        result.Formatters.Add(new TestXmlOutputFormatter());
        result.Formatters.Add(new TestStringOutputFormatter());

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        MediaTypeAssert.Equal("application/problem+json; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_ForProblemDetailsValue_UsesProblemDetailsXMLContentType_BasedOnAcceptHeader()
    {
        // Arrange
        var executor = CreateExecutor();

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext() { HttpContext = httpContext };
        httpContext.Request.Headers.Accept = "application/xml"; // This will not be used

        var result = new ObjectResult(new ProblemDetails())
        {
            ContentTypes = { "text/plain" }, // This will not be used
        };
        result.Formatters.Add(new TestJsonOutputFormatter());
        result.Formatters.Add(new TestXmlOutputFormatter()); // This will be chosen based on the Accept Headers "application/xml"
        result.Formatters.Add(new TestStringOutputFormatter());

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        MediaTypeAssert.Equal("application/problem+xml; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_NoContentTypeProvidedForProblemDetails_UsesDefaultContentTypes()
    {
        // Arrange
        var executor = CreateExecutor();

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext() { HttpContext = httpContext };

        var result = new ObjectResult(new ProblemDetails());
        result.Formatters.Add(new TestXmlOutputFormatter());  // This will be chosen based on the problem details added content type
        result.Formatters.Add(new TestJsonOutputFormatter());
        result.Formatters.Add(new TestStringOutputFormatter());

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        MediaTypeAssert.Equal("application/problem+xml; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_NoFormatterFound_Returns406()
    {
        // Arrange
        var executor = CreateExecutor();

        var actionContext = new ActionContext()
        {
            HttpContext = GetHttpContext(),
        };

        var result = new ObjectResult("input");

        // This formatter won't write anything
        result.Formatters = new FormatterCollection<IOutputFormatter>
            {
                new CannotWriteFormatter(),
            };

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        Assert.Equal(StatusCodes.Status406NotAcceptable, actionContext.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ExecuteAsync_FallsBackOnFormattersInOptions()
    {
        // Arrange
        var options = new MvcOptions();
        options.OutputFormatters.Add(new TestJsonOutputFormatter());

        var executor = CreateExecutor(options: options);

        var actionContext = new ActionContext()
        {
            HttpContext = GetHttpContext(),
        };

        var result = new ObjectResult("someValue");

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        Assert.Equal(
            "application/json; charset=utf-8",
            actionContext.HttpContext.Response.Headers.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWithNoFormatters()
    {
        // Arrange
        var expected = $"'{typeof(MvcOptions).FullName}.{nameof(MvcOptions.OutputFormatters)}' must not be " +
            $"empty. At least one '{typeof(IOutputFormatter).FullName}' is required to format a response.";
        var executor = CreateExecutor();
        var actionContext = new ActionContext
        {
            HttpContext = GetHttpContext(),
        };
        var result = new ObjectResult("some value");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.ExecuteAsync(actionContext, result));
        Assert.Equal(expected, exception.Message);
    }

    [Theory]
    [InlineData(new[] { "application/*" }, "application/*")]
    [InlineData(new[] { "application/xml", "application/*", "application/json" }, "application/*")]
    [InlineData(new[] { "application/*", "application/json" }, "application/*")]
    [InlineData(new[] { "*/*" }, "*/*")]
    [InlineData(new[] { "application/xml", "*/*", "application/json" }, "*/*")]
    [InlineData(new[] { "*/*", "application/json" }, "*/*")]
    [InlineData(new[] { "application/json", "application/*+json" }, "application/*+json")]
    [InlineData(new[] { "application/entiy+json;*", "application/json" }, "application/entiy+json;*")]
    public async Task ExecuteAsync_MatchAllContentType_Throws(string[] contentTypes, string invalidContentType)
    {
        // Arrange
        var result = new ObjectResult("input");

        var mediaTypes = new MediaTypeCollection();
        foreach (var contentType in contentTypes)
        {
            mediaTypes.Add(contentType);
        }

        result.ContentTypes = mediaTypes;

        var executor = CreateExecutor();

        var actionContext = new ActionContext() { HttpContext = new DefaultHttpContext() };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.ExecuteAsync(actionContext, result));

        var expectedMessage = string.Format(
            CultureInfo.CurrentCulture,
            "The content-type '{0}' added in the 'ContentTypes' property is " +
            "invalid. Media types which match all types or match all subtypes are not supported.",
          invalidContentType);
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    // Chrome & Opera
    [InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8", "application/json; charset=utf-8")]
    // IE
    [InlineData("text/html,application/xhtml+xml,*/*", "application/json; charset=utf-8")]
    // Firefox & Safari
    [InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8", "application/json; charset=utf-8")]
    // Misc
    [InlineData("*/*", @"application/json; charset=utf-8")]
    [InlineData("text/html,*/*;q=0.8,application/xml;q=0.9", "application/json; charset=utf-8")]
    public async Task ExecuteAsync_SelectDefaultFormatter_OnAllMediaRangeAcceptHeaderMediaType(
        string acceptHeader,
        string expectedContentType)
    {
        // Arrange
        var options = new MvcOptions();
        options.RespectBrowserAcceptHeader = false;

        var executor = CreateExecutor(options: options);

        var result = new ObjectResult("input");
        result.Formatters.Add(new TestJsonOutputFormatter());
        result.Formatters.Add(new TestXmlOutputFormatter());

        var actionContext = new ActionContext()
        {
            HttpContext = GetHttpContext(),
        };
        actionContext.HttpContext.Request.Headers.Accept = acceptHeader;

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        Assert.Equal(expectedContentType, actionContext.HttpContext.Response.Headers.ContentType);
    }

    [Theory]
    // Chrome & Opera
    [InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8", "application/xml; charset=utf-8")]
    // IE
    [InlineData("text/html,application/xhtml+xml,*/*", "application/json; charset=utf-8")]
    // Firefox & Safari
    [InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8", "application/xml; charset=utf-8")]
    // Misc
    [InlineData("*/*", @"application/json; charset=utf-8")]
    [InlineData("text/html,*/*;q=0.8,application/xml;q=0.9", "application/xml; charset=utf-8")]
    public async Task ObjectResult_PerformsContentNegotiation_OnAllMediaRangeAcceptHeaderMediaType(
        string acceptHeader,
        string expectedContentType)
    {
        // Arrange
        var options = new MvcOptions();
        options.RespectBrowserAcceptHeader = true;

        var executor = CreateExecutor(options: options);

        var result = new ObjectResult("input");
        result.Formatters.Add(new TestJsonOutputFormatter());
        result.Formatters.Add(new TestXmlOutputFormatter());

        var actionContext = new ActionContext()
        {
            HttpContext = GetHttpContext(),
        };
        actionContext.HttpContext.Request.Headers.Accept = acceptHeader;

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        var responseContentType = actionContext.HttpContext.Response.Headers.ContentType;
        MediaTypeAssert.Equal(expectedContentType, responseContentType);
    }

    [Fact]
    public async Task ObjectResult_NullValue()
    {
        // Arrange
        var executor = CreateExecutor();
        var result = new ObjectResult(value: null);
        var formatter = new TestJsonOutputFormatter();
        result.Formatters.Add(formatter);

        var actionContext = new ActionContext()
        {
            HttpContext = GetHttpContext(),
        };

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        var formatterContext = formatter.LastOutputFormatterContext;
        Assert.Null(formatterContext.Object);
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        return services;
    }

    private static HttpContext GetHttpContext()
    {
        var services = CreateServices();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }

    private static ObjectResultExecutor CreateExecutor(MvcOptions options = null)
    {
        options ??= new MvcOptions();
        var optionsAccessor = Options.Create(options);
        var selector = new DefaultOutputFormatterSelector(optionsAccessor, NullLoggerFactory.Instance);
        return new ObjectResultExecutor(selector, new TestHttpResponseStreamWriterFactory(), NullLoggerFactory.Instance, optionsAccessor);
    }

    private class CannotWriteFormatter : IOutputFormatter
    {
        public virtual bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            return false;
        }

        public virtual Task WriteAsync(OutputFormatterWriteContext context)
        {
            throw new NotImplementedException();
        }
    }

    private class TestJsonOutputFormatter : TextOutputFormatter
    {
        public TestJsonOutputFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/*+json"));

            SupportedEncodings.Add(Encoding.UTF8);
        }

        public OutputFormatterWriteContext LastOutputFormatterContext { get; private set; }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            LastOutputFormatterContext = context;
            return Task.FromResult(0);
        }
    }

    private class TestXmlOutputFormatter : TextOutputFormatter
    {
        public TestXmlOutputFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xml"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/*+xml"));

            SupportedEncodings.Add(Encoding.UTF8);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            return Task.FromResult(0);
        }
    }

    private class TestStringOutputFormatter : TextOutputFormatter
    {
        public TestStringOutputFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));

            SupportedEncodings.Add(Encoding.UTF8);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            return Task.FromResult(0);
        }
    }

    private class ServerContentTypeOnlyFormatter : OutputFormatter
    {
        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            // This test formatter matches if and only if the content type is specified
            // as "server defined". This lets tests identify what value the ObjectResultExecutor
            // passed for that flag.
            return context.ContentTypeIsServerDefined;
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            return Task.FromResult(0);
        }
    }
}
