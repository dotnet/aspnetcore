// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.ServerSentEvents;
using Microsoft.AspNetCore.Http.Metadata;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// Represents a result that writes a stream of server-sent events to the response.
/// </summary>
/// <typeparam name="T">The underlying type of the events emitted.</typeparam>
public sealed class ServerSentEventsResult<T> : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult
{
    private readonly IAsyncEnumerable<SseItem<T>> _events;

    /// <inheritdoc/>
    public int? StatusCode => StatusCodes.Status200OK;

    internal ServerSentEventsResult(IAsyncEnumerable<T> events, string? eventType)
    {
        _events = WrapEvents(events, eventType);
    }

    internal ServerSentEventsResult(IAsyncEnumerable<SseItem<T>> events)
    {
        _events = events;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache,no-store";
        httpContext.Response.Headers.Pragma = "no-cache";
        httpContext.Response.Headers.ContentEncoding = "identity";

        var bufferingFeature = httpContext.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
        bufferingFeature.DisableBuffering();

        var jsonOptions = httpContext.RequestServices.GetService<IOptions<JsonOptions>>()?.Value ?? new JsonOptions();

        // If the event type is string, we can skip JSON serialization
        // and directly use the SseFormatter's WriteAsync overload for strings.
        if (_events is IAsyncEnumerable<SseItem<string>> stringEvents)
        {
            await SseFormatter.WriteAsync(stringEvents, httpContext.Response.Body, httpContext.RequestAborted);
            return;
        }

        await SseFormatter.WriteAsync(_events, httpContext.Response.Body,
            (item, writer) => FormatSseItem(item, writer, jsonOptions),
            httpContext.RequestAborted);
    }

    private static void FormatSseItem(SseItem<T> item, IBufferWriter<byte> writer, JsonOptions jsonOptions)
    {
        if (item.Data is null)
        {
            writer.Write([]);
            return;
        }

        // Handle byte arrays byt writing them directly as strings.
        if (item.Data is byte[] byteArray)
        {
            writer.Write(byteArray);
            return;
        }

        // For non-string types, use JSON serialization with options from DI
        var runtimeType = item.Data.GetType();
        var jsonTypeInfo = jsonOptions.SerializerOptions.GetTypeInfo(typeof(T));

        // Use the appropriate JsonTypeInfo based on whether we need polymorphic serialization
        var typeInfo = jsonTypeInfo.ShouldUseWith(runtimeType)
            ? jsonTypeInfo
            : jsonOptions.SerializerOptions.GetTypeInfo(typeof(object));

        var json = JsonSerializer.SerializeToUtf8Bytes(item.Data, typeInfo);
        writer.Write(json);
    }

    private static async IAsyncEnumerable<SseItem<T>> WrapEvents(IAsyncEnumerable<T> events, string? eventType = null)
    {
        await foreach (var item in events)
        {
            yield return new SseItem<T>(item, eventType);
        }
    }

    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(SseItem<T>), contentTypes: ["text/event-stream"]));
    }
}
