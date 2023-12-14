// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Castle.Core.Internal;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests : RequestDelegateCreationTestBase
{
    [Fact]
    public async Task RequestDelegatePopulatesFromIFormFileCollectionParameter()
    {
        var source = """app.MapPost("/", (IFormFileCollection formFiles, HttpContext httpContext) => httpContext.Items["formFiles"] = formFiles);""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files, httpContext.Items["formFiles"]);
        var formFilesArgument = Assert.IsAssignableFrom<IFormFileCollection>(httpContext.Items["formFiles"]);
        Assert.NotNull(formFilesArgument!["file"]);

        var allAcceptsMetadata = endpoint.Metadata.OfType<IAcceptsMetadata>();
        var acceptsMetadata = Assert.Single(allAcceptsMetadata);

        Assert.NotNull(acceptsMetadata);
        Assert.Equal(new[] { "multipart/form-data" }, acceptsMetadata.ContentTypes);
        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromIFormFileCollectionParameterWithAttribute()
    {
        var source = """app.MapPost("/", ([FromForm] IFormFileCollection formFiles, HttpContext httpContext) => httpContext.Items["formFiles"] = formFiles);""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files, httpContext.Items["formFiles"]);
        var formFiles = Assert.IsAssignableFrom<IFormFileCollection>(httpContext.Items["formFiles"]);
        Assert.NotNull(formFiles["file"]);

        var allAcceptsMetadata = endpoint.Metadata.OfType<IAcceptsMetadata>();
        var acceptsMetadata = Assert.Single(allAcceptsMetadata);

        Assert.NotNull(acceptsMetadata);
        Assert.Equal(new[] { "multipart/form-data" }, acceptsMetadata.ContentTypes);
        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromIFormFileParameter()
    {
        var source = """app.MapPost("/", (IFormFile file, HttpContext httpContext) => httpContext.Items["formFiles"] = file);""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file"], httpContext.Items["formFiles"]);
        var fileArgument = Assert.IsAssignableFrom<IFormFile>(httpContext.Items["formFiles"]);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("file", fileArgument.Name);
        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromOptionalIFormFileParameter()
    {
        var source = """
app.MapPost("/", (IFormFile? file, HttpContext httpContext) =>
{
    if (file is not null)
    {
        httpContext.Items["formFiles"] = file;
    }
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file"], httpContext.Items["formFiles"]);
        var fileArgument = Assert.IsAssignableFrom<IFormFile>(httpContext.Items["formFiles"]);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("file", fileArgument.Name);
        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromMultipleRequiredIFormFileParameters()
    {
        var source = """
app.MapPost("/", (IFormFile file1, IFormFile file2, HttpContext httpContext) =>
{
    httpContext.Items["file1"] = file1;
    httpContext.Items["file2"] = file2;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent1 = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var fileContent2 = new StringContent("there", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent1, "file1", "file1.txt");
        form.Add(fileContent2, "file2", "file2.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file1"], httpContext.Items["file1"]);
        var file1Argument = Assert.IsAssignableFrom<IFormFile>(httpContext.Items["file1"]);
        Assert.Equal("file1.txt", file1Argument!.FileName);
        Assert.Equal("file1", file1Argument.Name);

        Assert.Equal(httpContext.Request.Form.Files["file2"], httpContext.Items["file2"]);
        var file2Argument = Assert.IsAssignableFrom<IFormFile>(httpContext.Items["file2"]);
        Assert.Equal("file2.txt", file2Argument!.FileName);
        Assert.Equal("file2", file2Argument.Name);
        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromOptionalMissingIFormFileParameter()
    {
        var source = """
app.MapPost("/", (IFormFile? file1, IFormFile? file2, HttpContext httpContext) =>
{
    httpContext.Items["file1"] = file1;
    httpContext.Items["file2"] = file2;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file1", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file1"], httpContext.Items["file1"]);
        Assert.NotNull(httpContext.Items["file1"]);

        Assert.Equal(httpContext.Request.Form.Files["file2"], httpContext.Items["file2"]);
        Assert.Null(httpContext.Items["file2"]);

        var allAcceptsMetadata = endpoint.Metadata.OfType<IAcceptsMetadata>();
        var acceptsMetadata = Assert.Single(allAcceptsMetadata);

        Assert.NotNull(acceptsMetadata);
        Assert.Equal(new[] { "multipart/form-data" }, acceptsMetadata.ContentTypes);
        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromIFormFileParameterWithMetadata()
    {
        var source = """app.MapPost("/", ([FromForm(Name = "my_file")] IFormFile file, HttpContext httpContext) => httpContext.Items["formFiles"] = file);""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "my_file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["my_file"], httpContext.Items["formFiles"]);
        var fileArgument = Assert.IsAssignableFrom<IFormFile>(httpContext.Items["formFiles"]);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("my_file", fileArgument.Name);
        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromIFormFileAndBoundParameter()
    {
        var source = """
app.MapPost("/", (IFormFile? file, TraceIdentifier traceId, HttpContext httpContext) =>
{
    httpContext.Items["formFiles"] = file;
    httpContext.Items["traceId"] = traceId;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.TraceIdentifier = "my-trace-id";

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file"], httpContext.Items["formFiles"]);
        var fileArgument = Assert.IsAssignableFrom<IFormFile>(httpContext.Items["formFiles"]);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("file", fileArgument.Name);

        var traceIdArgument = Assert.IsType<TraceIdentifier>(httpContext.Items["traceId"]);
        Assert.Equal("my-trace-id", traceIdArgument.Id);
        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestDelegateRejectsNonFormContent(bool shouldThrow)
    {
        var source = """app.MapPost("/", (IFormFile file, HttpContext httpContext) => httpContext.Items["formFiles"] = file);""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: CreateServiceProvider());

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/xml";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create((HttpContext context, IFormFile file) =>
        {
        }, new RequestDelegateFactoryOptions() { ThrowOnBadRequest = shouldThrow });
        var requestDelegate = factoryResult.RequestDelegate;

        var request = requestDelegate(httpContext);

        if (shouldThrow)
        {
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => request);
            Assert.Equal("Expected a supported form media type but got \"application/xml\".", ex.Message);
            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, ex.StatusCode);
        }
        else
        {
            await request;

            Assert.Equal(415, httpContext.Response.StatusCode);
            var logMessage = Assert.Single(TestSink.Writes);
            Assert.Equal(new EventId(7, "UnexpectedContentType"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Equal("Expected a supported form media type but got \"application/xml\".", logMessage.Message);
        }
    }

    [Fact]
    public async Task RequestDelegateSets400ResponseIfRequiredFileNotSpecified()
    {
        var source = """app.MapPost("/", (IFormFile file, HttpContext httpContext) => httpContext.Items["invoked"] = true);""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "some-other-file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Items["invoked"] = false;
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.False((bool)httpContext.Items["invoked"]);
        Assert.Equal(400, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromBothFormFileCollectionAndFormFileParameters()
    {
        var source = """
app.MapPost("/", (IFormFile file, IFormFileCollection formFiles, HttpContext httpContext) =>
{
    httpContext.Items["file"] = file;
    httpContext.Items["formFiles"] = formFiles;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files, httpContext.Items["formFiles"]);
        var formFilesArgument = Assert.IsType<FormFileCollection>(httpContext.Items["formFiles"]);
        Assert.NotNull(formFilesArgument!["file"]);

        Assert.Equal(httpContext.Request.Form.Files["file"], httpContext.Items["file"]);
        var fileArgument = Assert.IsAssignableFrom<IFormFile>(httpContext.Items["file"]);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("file", fileArgument.Name);

        var allAcceptsMetadata = endpoint.Metadata.OfType<IAcceptsMetadata>();
        var acceptsMetadata = Assert.Single(allAcceptsMetadata);

        Assert.NotNull(acceptsMetadata);
        Assert.Equal(new[] { "multipart/form-data" }, acceptsMetadata.ContentTypes);
    }

    [Theory]
    [InlineData("Authorization", "bearer my-token")]
    [InlineData("Cookie", ".AspNetCore.Auth=abc123")]
    public async Task RequestDelegatePopulatesFromIFormFileParameterIfRequestContainsSecureHeader(
        string headerName,
        string headerValue)
    {
        var source = """
app.MapPost("/", (IFormFile? file, TraceIdentifier traceId, HttpContext httpContext) =>
{
    httpContext.Items["file"] = file;
    httpContext.Items["traceId"] = traceId;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers[headerName] = headerValue;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.TraceIdentifier = "my-trace-id";

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file"], httpContext.Items["file"]);
        var fileArgument = Assert.IsAssignableFrom<IFormFile>(httpContext.Items["file"]);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("file", fileArgument.Name);

        var traceIdArgument = Assert.IsAssignableFrom<TraceIdentifier>(httpContext.Items["traceId"]);
        Assert.Equal("my-trace-id", traceIdArgument.Id);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromIFormFileParameterIfRequestHasClientCertificate()
    {
        var source = """
app.MapPost("/", (IFormFile? file, TraceIdentifier traceId, HttpContext httpContext) =>
{
    httpContext.Items["file"] = file;
    httpContext.Items["traceId"] = traceId;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.TraceIdentifier = "my-trace-id";

#pragma warning disable SYSLIB0026 // Type or member is obsolete
        var clientCertificate = new X509Certificate2();
#pragma warning restore SYSLIB0026 // Type or member is obsolete

        httpContext.Features.Set<ITlsConnectionFeature>(new TlsConnectionFeature(clientCertificate));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file"], httpContext.Items["file"]);
        var fileArgument = Assert.IsAssignableFrom<IFormFile>(httpContext.Items["file"]);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("file", fileArgument.Name);

        var traceIdArgument = Assert.IsAssignableFrom<TraceIdentifier>(httpContext.Items["traceId"]);
        Assert.Equal("my-trace-id", traceIdArgument.Id);
    }

    public static TheoryData<HttpContent, string> FormContent
    {
        get
        {
            var dataset = new TheoryData<HttpContent, string>();

            var multipartFormData = new MultipartFormDataContent("some-boundary");
            multipartFormData.Add(new StringContent("hello"), "message");
            multipartFormData.Add(new StringContent("foo"), "name");
            dataset.Add(multipartFormData, "multipart/form-data;boundary=some-boundary");

            var urlEncondedForm = new FormUrlEncodedContent(new Dictionary<string, string> { ["message"] = "hello", ["name"] = "foo" });
            dataset.Add(urlEncondedForm, "application/x-www-form-urlencoded");

            return dataset;
        }
    }

    [Theory]
    [MemberData(nameof(FormContent))]
    public async Task RequestDelegatePopulatesFromIFormCollectionParameter(HttpContent content, string contentType)
    {
        var source = """
app.MapPost("/", (IFormCollection formFiles, HttpContext httpContext) =>
{
    httpContext.Items["formFiles"] = formFiles;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = contentType;
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form, httpContext.Items["formFiles"]);
        var formArgument = Assert.IsAssignableFrom<IFormCollection>(httpContext.Items["formFiles"]);
        Assert.NotNull(formArgument);
        Assert.Collection(formArgument!,
            (item) =>
            {
                Assert.Equal("message", item.Key);
                Assert.Equal("hello", item.Value);
            },
            (item) =>
            {
                Assert.Equal("name", item.Key);
                Assert.Equal("foo", item.Value);
            });

        var allAcceptsMetadata = endpoint.Metadata.OfType<IAcceptsMetadata>();
        var acceptsMetadata = Assert.Single(allAcceptsMetadata);

        Assert.NotNull(acceptsMetadata);
        Assert.Equal(new[] { "multipart/form-data", "application/x-www-form-urlencoded" }, acceptsMetadata.ContentTypes);
    }

    [Theory]
    [MemberData(nameof(FormContent))]
    public async Task RequestDelegatePopulatesFromIFormCollectionParameterWithAttribute(HttpContent content, string contentType)
    {
        var source = """
app.MapPost("/", ([FromForm] IFormCollection formFiles, HttpContext httpContext) =>
{
    httpContext.Items["formFiles"] = formFiles;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = contentType;
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form, httpContext.Items["formFiles"]);
        var formArgument = Assert.IsAssignableFrom<IFormCollection>(httpContext.Items["formFiles"]);
        Assert.NotNull(formArgument);
        Assert.Collection(formArgument!,
            (item) =>
            {
                Assert.Equal("message", item.Key);
                Assert.Equal("hello", item.Value);
            },
            (item) =>
            {
                Assert.Equal("name", item.Key);
                Assert.Equal("foo", item.Value);
            });

        var allAcceptsMetadata = endpoint.Metadata.OfType<IAcceptsMetadata>();
        var acceptsMetadata = Assert.Single(allAcceptsMetadata);

        Assert.NotNull(acceptsMetadata);
        Assert.Equal(new[] { "multipart/form-data", "application/x-www-form-urlencoded" }, acceptsMetadata.ContentTypes);
    }

    [Theory]
    [MemberData(nameof(FormContent))]
    public async Task RequestDelegatePopulatesFromOptionalFormParameter(HttpContent content, string contentType)
    {
        var source = """
app.MapPost("/", ([FromForm] string? message, HttpContext httpContext) =>
{
    httpContext.Items["message"] = message;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = contentType;
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form["message"][0], httpContext.Items["message"]);
    }

    [Theory]
    [MemberData(nameof(FormContent))]
    public async Task RequestDelegatePopulatesFromMultipleRequiredFormParameters(HttpContent content, string contentType)
    {
        var source = """
app.MapPost("/", ([FromForm] string message, [FromForm] string name, HttpContext httpContext) =>
{
    httpContext.Items["message"] = message;
    httpContext.Items["name"] = name;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = contentType;
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form["message"][0], httpContext.Items["message"]);
        Assert.NotNull(httpContext.Items["message"]);

        Assert.Equal(httpContext.Request.Form["name"][0], httpContext.Items["name"]);
        Assert.NotNull(httpContext.Items["name"]);
    }

    [Theory]
    [MemberData(nameof(FormContent))]
    public async Task RequestDelegatePopulatesFromOptionalMissingFormParameter(HttpContent content, string contentType)
    {
        var source = """
app.MapPost("/", ([FromForm] string? message, [FromForm] string? additionalMessage, HttpContext httpContext) =>
{
    httpContext.Items["message"] = message;
    httpContext.Items["additionalMessage"] = additionalMessage;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = contentType;
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form["message"][0], httpContext.Items["message"]);
        Assert.NotNull(httpContext.Items["message"]);
        Assert.Null(httpContext.Items["additionalMessage"]);
    }

    [Theory]
    [MemberData(nameof(FormContent))]
    public async Task RequestDelegatePopulatesFromFormParameterWithMetadata(HttpContent content, string contentType)
    {
        var source = """
app.MapPost("/", ([FromForm(Name = "message")] string text, HttpContext httpContext) =>
{
    httpContext.Items["message"] = text;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = contentType;
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form["message"][0], httpContext.Items["message"]);
        Assert.NotNull(httpContext.Items["message"]);
    }

    [Theory]
    [MemberData(nameof(FormContent))]
    public async Task RequestDelegatePopulatesFromFormAndBoundParameter(HttpContent content, string contentType)
    {
        var source = """
app.MapPost("/", ([FromForm] string? message, TraceIdentifier traceId, HttpContext httpContext) =>
{
    httpContext.Items["message"] = message;
    httpContext.Items["traceId"] = traceId;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = contentType;
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.TraceIdentifier = "my-trace-id";
        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form["message"][0], httpContext.Items["message"]);
        Assert.NotNull(httpContext.Items["message"]);

        var traceIdArgument = Assert.IsType<TraceIdentifier>(httpContext.Items["traceId"]);
        Assert.Equal("my-trace-id", traceIdArgument.Id);
    }

    public static IEnumerable<object[]> FormAndFormFileParametersDelegates
    {
        get
        {
            var source = """
void TestAction(HttpContext context, IFormCollection form, IFormFileCollection formFiles)
{
    context.Items["FormFilesArgument"] = formFiles;
    context.Items["FormArgument"] = form;
}
""";

            var sourceDifferentOrder = """
void TestAction(HttpContext context, IFormFileCollection formFiles, IFormCollection form)
{
    context.Items["FormFilesArgument"] = formFiles;
    context.Items["FormArgument"] = form;
}
""";

            return new List<object[]>
            {
                new object[] { source },
                new object[] { sourceDifferentOrder },
            };
        }
    }

    [Theory]
    [MemberData(nameof(FormAndFormFileParametersDelegates))]
    public async Task RequestDelegatePopulatesFromBothIFormCollectionAndIFormFileParameters(string innerSource)
    {
        var source = $"""
{innerSource}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");
        form.Add(new StringContent("foo"), "name");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        var formFilesArgument = Assert.IsAssignableFrom<FormFileCollection>(httpContext.Items["FormFilesArgument"]);
        var formArgument = Assert.IsAssignableFrom<IFormCollection>(httpContext.Items["FormArgument"]);

        Assert.Equal(httpContext.Request.Form.Files, formFilesArgument);
        Assert.NotNull(formFilesArgument!["file"]);
        Assert.Equal("file.txt", formFilesArgument!["file"]!.FileName);

        Assert.Equal(httpContext.Request.Form, formArgument);
        Assert.NotNull(formArgument);
        Assert.Collection(formArgument!,
            (item) =>
            {
                Assert.Equal("name", item.Key);
                Assert.Equal("foo", item.Value);
            });

        var allAcceptsMetadata = endpoint.Metadata.OfType<IAcceptsMetadata>();
        Assert.Collection(allAcceptsMetadata,
            (m) => Assert.Equal(new[] { "multipart/form-data" }, m.ContentTypes));
    }

    [Theory]
    [MemberData(nameof(FormContent))]
    public async Task RequestDelegateSets400ResponseIfRequiredFormItemNotSpecified(HttpContent content, string contentType)
    {
        var source = """
app.MapPost("/", ([FromForm] string unknownParameter, HttpContext httpContext) => httpContext.Items["invoked"] = true);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Items["invoked"] = false;
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = contentType;
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.False((bool)httpContext.Items["invoked"]);
        Assert.Equal(400, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task RequestDelegatePopulatesTryParsableParametersFromForm()
    {
        var source = """
app.MapPost("/", (HttpContext httpContext, [FromForm] MyTryParseRecord tryParsable) =>
{
    httpContext.Items["tryParsable"] = tryParsable;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["tryParsable"] = "https://example.org"
        });

        await endpoint.RequestDelegate(httpContext);

        var content = Assert.IsType<MyTryParseRecord>(httpContext.Items["tryParsable"]);
        Assert.Equal(new Uri("https://example.org"), content.Uri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestDelegateLogsIOExceptionsForFormAsDebugDoesNotAbortAndNeverThrows(bool throwOnBadRequests)
    {
        var source = """
void TestAction(HttpContext httpContext, IFormFile file)
{
    httpContext.Items["invoked"] = true;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = throwOnBadRequests);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var ioException = new IOException();

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new ExceptionThrowingRequestBodyStream(ioException);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Null(httpContext.Items["invoked"]);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(1, "RequestBodyIOException"), logMessage.EventId);
        Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        Assert.Equal("Reading the request body failed with an IOException.", logMessage.Message);
        Assert.Same(ioException, logMessage.Exception);
    }

    [Fact]
    public async Task RequestDelegateLogsMalformedFormAsDebugAndSets400Response()
    {
        var source = """
void TestAction(HttpContext httpContext, IFormFile file)
{
    httpContext.Items["invoked"] = true;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider();
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Request.Headers["Content-Length"] = "2049";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(new string('x', 2049)));
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Null(httpContext.Items["invoked"]);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(8, "InvalidFormRequestBody"), logMessage.EventId);
        Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        Assert.Equal(@"Failed to read parameter ""IFormFile file"" from the request body as form.", logMessage.Message);
        Assert.IsType<InvalidDataException>(logMessage.Exception);
    }

    [Fact]
    public async Task RequestDelegateThrowsForMalformedFormIfThrowOnBadRequest()
    {
        var source = """
void TestAction(HttpContext httpContext, IFormFile file)
{
    httpContext.Items["invoked"] = true;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Request.Headers["Content-Length"] = "2049";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(new string('x', 2049)));
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));

        Assert.Null(httpContext.Items["invoked"]);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Failed to read parameter ""IFormFile file"" from the request body as form.", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
        Assert.IsType<InvalidDataException>(badHttpRequestException.InnerException);
    }

    [Fact]
    public async Task RequestDelegateValidateGeneratedFormCode()
    {
        var source = """
void TestAction(HttpContext httpContext, IFormFile file, IFormFileCollection fileCollection, IFormCollection collection, [FromForm] MyTryParseRecord tryParseRecord)
{
    httpContext.Items["invoked"] = true;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);

        await VerifyAgainstBaselineUsingFile(compilation);
    }
}
