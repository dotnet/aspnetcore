// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class RuntimeCreationTests : RequestDelegateCreationTests
{
    [Fact]
    public async Task SupportsBindingComplexTypeFromForm_UrlEncoded()
    {
        var source = """
app.MapPost("/", ([FromForm] Todo todo) => Results.Ok(todo));
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["Id"] = "1", ["Name"] = "Write tests", ["IsComplete"] = "true" });
        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());

        await VerifyResponseJsonBodyAsync<Todo>(httpContext, (todo) =>
        {
            Assert.Equal(1, todo.Id);
            Assert.Equal("Write tests", todo.Name);
            Assert.True(todo.IsComplete);
        });
    }

    [Fact]
    public async Task SupportsBindingComplexTypeFromForm_Multipart()
    {
        var source = """
app.MapPost("/", ([FromForm] Todo todo) => Results.Ok(todo));
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        var content = new MultipartFormDataContent("some-boundary");
        content.Add(new StringContent("1"), "Id");
        content.Add(new StringContent("Write tests"), "Name");
        content.Add(new StringContent("true"), "IsComplete");

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());

        await VerifyResponseJsonBodyAsync<Todo>(httpContext, (todo) =>
        {
            Assert.Equal(1, todo.Id);
            Assert.Equal("Write tests", todo.Name);
            Assert.True(todo.IsComplete);
        });
    }

    [Fact]
    public async Task SupportsBindingDictionaryFromForm_UrlEncoded()
    {
        var source = """
app.MapPost("/", ([FromForm] Dictionary<string, bool> elements) => Results.Ok(elements));
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["[foo]"] = "true", ["[bar]"] = "false", ["[baz]"] = "true" });
        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());

        await VerifyResponseJsonBodyAsync<Dictionary<string, bool>>(httpContext, (elements) =>
        {
            Assert.Equal(3, elements.Count);
            Assert.True(elements["foo"]);
            Assert.False(elements["bar"]);
            Assert.True(elements["baz"]);
        });
    }

    [Fact]
    public async Task SupportsBindingDictionaryFromForm_Multipart()
    {
        var source = """
app.MapPost("/", ([FromForm] Dictionary<string, bool> elements) => Results.Ok(elements));
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        var content = new MultipartFormDataContent("some-boundary");
        content.Add(new StringContent("true"), "[foo]");
        content.Add(new StringContent("false"), "[bar]");
        content.Add(new StringContent("true"), "[baz]");

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());

        await VerifyResponseJsonBodyAsync<Dictionary<string, bool>>(httpContext, (elements) =>
        {
            Assert.Equal(3, elements.Count);
            Assert.True(elements["foo"]);
            Assert.False(elements["bar"]);
            Assert.True(elements["baz"]);
        });
    }

    [Fact]
    public async Task SupportsBindingInvalidDictionaryFromForm_Multipart()
    {
        var source = """
app.MapPost("/", ([FromForm] Dictionary<string, bool> elements) => Results.Ok(elements));
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        var content = new MultipartFormDataContent("some-boundary");
        content.Add(new StringContent("not-a-bool"), "[foo]");
        content.Add(new StringContent("1"), "[bar]");
        content.Add(new StringContent("2"), "[baz]");

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);

        var logs = TestSink.Writes.ToArray();

        var log = Assert.Single(logs);

        Assert.Equal(new EventId(10, "FormMappingFailed"), log.EventId);
        Assert.Equal(LogLevel.Debug, log.LogLevel);
        Assert.Equal(@"Failed to bind parameter ""Dictionary<string, bool> elements"" from the request body as form.", log.Message);
        var log1Values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(log.State);
        Assert.Equal("Dictionary<string, bool>", log1Values[0].Value);
        Assert.Equal("elements", log1Values[1].Value);
    }

    [Fact]
    public async Task SupportsBindingListFromForm_UrlEncoded()
    {
        var source = """
app.MapPost("/", ([FromForm] List<int> elements) => Results.Ok(elements));
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["[0]"] = "1", ["[1]"] = "3", ["[2]"] = "5" });
        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());

        await VerifyResponseJsonBodyAsync<List<int>>(httpContext, (elements) =>
        {
            Assert.Equal(3, elements.Count);
            Assert.Equal(1, elements[0]);
            Assert.Equal(3, elements[1]);
            Assert.Equal(5, elements[2]);
        });
    }

    [Fact]
    public async Task SupportsBindingListFromForm_Multipart()
    {
        var source = """
app.MapPost("/", ([FromForm] List<int> elements) => Results.Ok(elements));
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        var content = new MultipartFormDataContent("some-boundary");
        content.Add(new StringContent("1"), "[0]");
        content.Add(new StringContent("3"), "[1]");
        content.Add(new StringContent("5"), "[2]");

        var stream = new MemoryStream();
        await content.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.NotNull(endpoint.Metadata.OfType<IAntiforgeryMetadata>().SingleOrDefault());

        await VerifyResponseJsonBodyAsync<List<int>>(httpContext, (elements) =>
        {
            Assert.Equal(3, elements.Count);
            Assert.Equal(1, elements[0]);
            Assert.Equal(3, elements[1]);
            Assert.Equal(5, elements[2]);
        });
    }
}
