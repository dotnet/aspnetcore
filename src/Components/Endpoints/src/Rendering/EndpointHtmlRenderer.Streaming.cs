// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class EndpointHtmlRenderer
{
    private const string _streamingRenderingFramingHeaderName = "ssr-framing";
    private TextWriter? _streamingUpdatesWriter;
    private HashSet<int>? _visitedComponentIdsInCurrentStreamingBatch;
    private string? _ssrFramingCommentMarkup;
    private bool _isHandlingErrors;
    private bool _isReExecuted;

    public void InitializeStreamingRenderingFraming(HttpContext httpContext, bool isErrorHandler, bool isReExecuted)
    {
        _isHandlingErrors = isErrorHandler;
        _isReExecuted = isReExecuted;
        if (!isReExecuted && IsProgressivelyEnhancedNavigation(httpContext.Request))
        {
            var id = Guid.NewGuid().ToString();
            httpContext.Response.Headers.Add(_streamingRenderingFramingHeaderName, id);
            _ssrFramingCommentMarkup = $"<!--{id}-->";
        }
        else
        {
            _ssrFramingCommentMarkup = string.Empty;
        }
    }

    // We do not want the debugger to consider NavigationExceptions caught by this method as user-unhandled.
    [DebuggerDisableUserUnhandledExceptions]
    public async Task SendStreamingUpdatesAsync(HttpContext httpContext, Task untilTaskCompleted, TextWriter writer)
    {
        // Important: do not introduce any 'await' statements in this method above the point where we write
        // the SSR framing markers, otherwise batches may be emitted before the framing makers, and then the
        // response would be invalid. See the comment below indicating the point where we intentionally yield
        // the sync context to allow SSR batches to begin being emitted.

        SetHttpContext(httpContext);

        if (_streamingUpdatesWriter is not null)
        {
            // The framework is the only caller, so it's OK to have a nonobvious restriction like this
            throw new InvalidOperationException($"{nameof(SendStreamingUpdatesAsync)} can only be called once.");
        }

        if (_ssrFramingCommentMarkup is null)
        {
            throw new InvalidOperationException("Cannot begin streaming rendering because no framing header was set.");
        }

        _streamingUpdatesWriter = writer;

        try
        {
            writer.Write(_ssrFramingCommentMarkup);
            EmitInitializersIfNecessary(httpContext, writer);

            // At this point we yield the sync context. SSR batches may then be emitted at any time.
            await writer.FlushAsync();
            await untilTaskCompleted;
        }
        catch (NavigationException navigationException)
        {
            HandleNavigationAfterResponseStarted(writer, httpContext, navigationException.Location);
        }
        catch (Exception ex)
        {
            // Rethrowing also informs the debugger that this exception should be considered user-unhandled unlike NavigationExceptions,
            // but calling BreakForUserUnhandledException here allows the debugger to break before we modify the HttpContext.
            Debugger.BreakForUserUnhandledException(ex);

            // Theoretically it might be possible to let the error middleware run, capture the output,
            // then emit it in a special format so the JS code can display the error page. However
            // for now we're not going to support that and will simply emit a message.
            HandleExceptionAfterResponseStarted(_httpContext, writer, ex);
            await writer.FlushAsync(); // Important otherwise the client won't receive the error message, as we're about to fail the pipeline
            await _httpContext.Response.CompleteAsync();
            throw;
        }
    }

    internal void EmitInitializersIfNecessary(HttpContext httpContext, TextWriter writer)
    {
        if (_options.JavaScriptInitializers != null &&
            !IsProgressivelyEnhancedNavigation(httpContext.Request))
        {
            var initializersBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(_options.JavaScriptInitializers));
            writer.Write("<!--Blazor-Web-Initializers:");
            writer.Write(initializersBase64);
            writer.Write("-->");
        }
    }

    private void SendBatchAsStreamingUpdate(in RenderBatch renderBatch, TextWriter writer)
    {
        var count = renderBatch.UpdatedComponents.Count;
        if (count > 0)
        {
            writer.Write("<blazor-ssr>");

            // Each time we transmit the HTML for a component, we also transmit the HTML for its descendants.
            // So, if we transmitted *every* component in the batch separately, there would be a lot of duplication.
            // The subtrees projected from each component would overlap a lot.
            //
            // To avoid duplicated HTML transmission and unnecessary work on the client, we want to pick a subset
            // of updated components such that, when we transmit that subset with their descendants, it includes
            // every updated component without any duplication.
            //
            // This is quite easy if we first sort the list into depth order. As long as we process parents before
            // their descendants, we can keep a log of the descendants we rendered, and then skip over those if we
            // see them later in the list. This also implicitly handles the case where a batch contains the same
            // root component multiple times (we only want to emit its HTML once).

            // First, get a list of updated components in depth order
            // We'll stackalloc a buffer if it's small, otherwise take a buffer on the heap
            var bufSizeRequired = count * Marshal.SizeOf<ComponentIdAndDepth>();
            var componentIdsInDepthOrder = bufSizeRequired < 1024
                ? MemoryMarshal.Cast<byte, ComponentIdAndDepth>(stackalloc byte[bufSizeRequired])
                : new ComponentIdAndDepth[count];
            for (var i = 0; i < count; i++)
            {
                var componentId = renderBatch.UpdatedComponents.Array[i].ComponentId;
                componentIdsInDepthOrder[i] = new(componentId, GetComponentDepth(componentId));
            }
            MemoryExtensions.Sort(componentIdsInDepthOrder, static (left, right) => left.Depth - right.Depth);

            // Reset the component rendering tracker. This is safe to share as an instance field because batch-rendering
            // is synchronous only one batch can be rendered at a time.
            if (_visitedComponentIdsInCurrentStreamingBatch is null)
            {
                _visitedComponentIdsInCurrentStreamingBatch = new();
            }
            else
            {
                _visitedComponentIdsInCurrentStreamingBatch.Clear();
            }

            // Now process the list, skipping any we've already visited in an earlier iteration
            var isEnhancedNavigation = IsProgressivelyEnhancedNavigation(_httpContext.Request);
            for (var i = 0; i < componentIdsInDepthOrder.Length; i++)
            {
                var componentId = componentIdsInDepthOrder[i].ComponentId;
                if (_visitedComponentIdsInCurrentStreamingBatch.Contains(componentId))
                {
                    continue;
                }

                // Of the components that updated, we want to emit the roots of all the streaming subtrees, and not
                // any non-streaming ancestors. There's no point emitting non-streaming ancestor content since there
                // are no markers in the document to receive it. Also we don't want to call WriteComponentHtml for
                // nonstreaming ancestors, as that would make us skip over their descendants who may in fact be the
                // roots of streaming subtrees.
                var componentState = (EndpointComponentState)GetComponentState(componentId);
                if (!componentState.StreamRendering)
                {
                    continue;
                }

                // This format relies on the component producing well-formed markup (i.e., it can't have a
                // </template> at the top level without a preceding matching <template>). Alternatively we
                // could look at using a custom TextWriter that does some extra encoding of all the content
                // as it is being written out.
                writer.Write($"<template blazor-component-id=\"");
                writer.Write(componentId);
                writer.Write(isEnhancedNavigation ? "\" enhanced-nav=\"true\">" : "\">");

                // We don't need boundary markers at the top-level since the info is on the <template> anyway.
                WriteComponentHtml(componentId, writer, allowBoundaryMarkers: false);

                writer.Write("</template>");
            }

            writer.Write("<blazor-ssr-end></blazor-ssr-end></blazor-ssr>");
            writer.Write(_ssrFramingCommentMarkup);
        }
    }

    private int GetComponentDepth(int componentId)
    {
        // Regard root components as depth 0, their immediate children as 1, etc.
        var componentState = GetComponentState(componentId);
        var depth = 0;
        while (componentState.ParentComponentState is { } parentComponentState)
        {
            depth++;
            componentState = parentComponentState;
        }

        return depth;
    }

    internal static bool ShouldShowDetailedErrors(HttpContext httpContext)
    {
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var options = httpContext.RequestServices.GetRequiredService<IOptions<RazorComponentsServiceOptions>>();
        var showDetailedErrors = env.IsDevelopment() || options.Value.DetailedErrors;
        return showDetailedErrors;
    }

    private static void HandleExceptionAfterResponseStarted(HttpContext httpContext, TextWriter writer, Exception exception)
    {
        // We already started the response so we have no choice but to return a 200 with HTML and will
        // have to communicate the error information within that
        var showDetailedErrors = ShouldShowDetailedErrors(httpContext);
        var message = showDetailedErrors
            ? exception.ToString()
            : "There was an unhandled exception on the current request. For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json'";

        writer.Write("<blazor-ssr><template type=\"error\">");
        writer.Write(HtmlEncoder.Default.Encode(message));
        writer.Write("</template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>");
    }

    private static void HandleNotFoundAfterResponseStarted(TextWriter writer, HttpContext httpContext, string notFoundUrl)
    {
        writer.Write("<blazor-ssr><template type=\"not-found\"");
        WriteResponseTemplate(writer, httpContext, notFoundUrl, useEnhancedNav: true);
    }

    private static void HandleNavigationAfterResponseStarted(TextWriter writer, HttpContext httpContext, string destinationUrl)
    {
        writer.Write("<blazor-ssr><template type=\"redirection\"");
        bool useEnhancedNav = IsProgressivelyEnhancedNavigation(httpContext.Request);
        WriteResponseTemplate(writer, httpContext, destinationUrl, useEnhancedNav);
    }

    private static void WriteResponseTemplate(TextWriter writer, HttpContext httpContext, string destinationUrl, bool useEnhancedNav)
    {
        if (HttpMethods.IsPost(httpContext.Request.Method))
        {
            writer.Write(" from=\"form-post\"");
        }

        if (useEnhancedNav)
        {
            writer.Write(" enhanced=\"true\"");
        }

        writer.Write(">");
        writer.Write(HtmlEncoder.Default.Encode(OpaqueRedirection.CreateProtectedRedirectionUrl(httpContext, destinationUrl)));
        writer.Write("</template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>");
    }

    protected override void WriteComponentHtml(int componentId, TextWriter output)
        => WriteComponentHtml(componentId, output, allowBoundaryMarkers: true);

    protected override void RenderChildComponent(TextWriter output, ref RenderTreeFrame componentFrame)
    {
        var componentId = componentFrame.ComponentId;
        var sequenceAndKey = new SequenceAndKey(componentFrame.Sequence, componentFrame.ComponentKey);
        WriteComponentHtml(componentId, output, allowBoundaryMarkers: true, sequenceAndKey);
    }

    private void WriteComponentHtml(int componentId, TextWriter output, bool allowBoundaryMarkers, SequenceAndKey sequenceAndKey = default)
    {
        _visitedComponentIdsInCurrentStreamingBatch?.Add(componentId);

        var componentState = (EndpointComponentState)GetComponentState(componentId);
        var renderBoundaryMarkers = allowBoundaryMarkers && componentState.StreamRendering;

        ComponentEndMarker? endMarkerOrNull = default;

        if (componentState.Component is SSRRenderModeBoundary boundary)
        {
            var marker = boundary.ToMarker(_httpContext, sequenceAndKey.Sequence, sequenceAndKey.Key);
            endMarkerOrNull = marker.ToEndMarker();

            if (!_httpContext.Response.HasStarted && marker.Type is ComponentMarker.ServerMarkerType or ComponentMarker.AutoMarkerType)
            {
                _httpContext.Response.Headers.CacheControl = "no-cache, no-store, max-age=0";
            }

            if (marker.Type is ComponentMarker.WebAssemblyMarkerType or ComponentMarker.AutoMarkerType)
            {
                if (_httpContext.RequestServices.GetRequiredService<WebAssemblySettingsEmitter>().TryGetSettingsOnce(out var settings))
                {
                    var settingsJson = JsonSerializer.Serialize(settings, ServerComponentSerializationSettings.JsonSerializationOptions);
                    output.Write($"<!--Blazor-WebAssembly:{settingsJson}-->");
                }
            }

            var serializedStartRecord = JsonSerializer.Serialize(marker, ServerComponentSerializationSettings.JsonSerializationOptions);
            output.Write("<!--Blazor:");
            output.Write(serializedStartRecord);
            output.Write("-->");
        }

        if (renderBoundaryMarkers)
        {
            output.Write("<!--bl:");
            output.Write(componentId);
            output.Write("-->");
        }

        base.WriteComponentHtml(componentId, output);

        if (renderBoundaryMarkers)
        {
            output.Write("<!--/bl:");
            output.Write(componentId);
            output.Write("-->");
        }

        if (endMarkerOrNull is { } endMarker)
        {
            var serializedEndRecord = JsonSerializer.Serialize(endMarker, ServerComponentSerializationSettings.JsonSerializationOptions);
            output.Write("<!--Blazor:");
            output.Write(serializedEndRecord);
            output.Write("-->");
        }
    }

    internal static bool IsProgressivelyEnhancedNavigation(HttpRequest request)
    {
        // For enhanced nav, the Blazor JS code controls the "accept" header precisely, so we can be very specific about the format
        var accept = request.Headers.Accept;
        return accept.Count == 1 && string.Equals(accept[0]!, "text/html; blazor-enhanced-nav=on", StringComparison.Ordinal);
    }

    private readonly struct ComponentIdAndDepth
    {
        public int ComponentId { get; }

        public int Depth { get; }

        public ComponentIdAndDepth(int componentId, int depth)
        {
            ComponentId = componentId;
            Depth = depth;
        }
    }

    private readonly struct SequenceAndKey
    {
        public int Sequence { get; }

        public object? Key { get; }

        public SequenceAndKey(int sequence, object? key)
        {
            Sequence = sequence;
            Key = key;
        }
    }
}
