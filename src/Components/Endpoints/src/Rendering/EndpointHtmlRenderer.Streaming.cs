// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class EndpointHtmlRenderer
{
    private TextWriter? _streamingUpdatesWriter;
    private HashSet<int>? _visitedComponentIdsInCurrentStreamingBatch;

    public async Task SendStreamingUpdatesAsync(HttpContext httpContext, Task untilTaskCompleted, TextWriter writer)
    {
        SetHttpContext(httpContext);

        if (_streamingUpdatesWriter is not null)
        {
            // The framework is the only caller, so it's OK to have a nonobvious restriction like this
            throw new InvalidOperationException($"{nameof(SendStreamingUpdatesAsync)} can only be called once.");
        }

        _streamingUpdatesWriter = writer;

        try
        {
            await writer.FlushAsync(); // Make sure the initial HTML was sent
            await untilTaskCompleted;
        }
        catch (NavigationException navigationException)
        {
            HandleNavigationAfterResponseStarted(writer, navigationException.Location);
        }
        catch (Exception ex)
        {
            HandleExceptionAfterResponseStarted(_httpContext, writer, ex);

            // The rest of the pipeline can treat this as a regular unhandled exception
            // TODO: Is this really right? I think we'll terminate the response in an invalid way.
            throw;
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
            for (var i = 0; i < componentIdsInDepthOrder.Length; i++)
            {
                var componentId = componentIdsInDepthOrder[i].ComponentId;
                if (_visitedComponentIdsInCurrentStreamingBatch.Contains(componentId))
                {
                    continue;
                }

                // This format relies on the component producing well-formed markup (i.e., it can't have a
                // </template> at the top level without a preceding matching <template>). Alternatively we
                // could look at using a custom TextWriter that does some extra encoding of all the content
                // as it is being written out.
                writer.Write($"<template blazor-component-id=\"");
                writer.Write(componentId);
                writer.Write("\">");

                // We don't need boundary markers at the top-level since the info is on the <template> anyway.
                WriteComponentHtml(componentId, writer, allowBoundaryMarkers: false);

                writer.Write("</template>");
            }

            writer.Write("</blazor-ssr>");
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

    private static void HandleExceptionAfterResponseStarted(HttpContext httpContext, TextWriter writer, Exception exception)
    {
        // We already started the response so we have no choice but to return a 200 with HTML and will
        // have to communicate the error information within that
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var options = httpContext.RequestServices.GetRequiredService<IOptions<RazorComponentsEndpointsOptions>>();
        var showDetailedErrors = env.IsDevelopment() || options.Value.DetailedErrors;
        var message = showDetailedErrors
            ? exception.ToString()
            : "There was an unhandled exception on the current request. For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json'";

        writer.Write("<template blazor-type=\"exception\">");
        writer.Write(message);
        writer.Write("</template>");
    }

    private static void HandleNavigationAfterResponseStarted(TextWriter writer, string destinationUrl)
    {
        writer.Write("<template blazor-type=\"redirection\">");
        writer.Write(destinationUrl);
        writer.Write("</template>");
    }

    protected override void WriteComponentHtml(int componentId, TextWriter output)
        => WriteComponentHtml(componentId, output, allowBoundaryMarkers: true);

    private void WriteComponentHtml(int componentId, TextWriter output, bool allowBoundaryMarkers)
    {
        _visitedComponentIdsInCurrentStreamingBatch?.Add(componentId);

        var componentState = (EndpointComponentState)GetComponentState(componentId);
        var renderBoundaryMarkers = allowBoundaryMarkers && componentState.StreamRendering;

        // TODO: It's not clear that we actually want to emit the interactive component markers using this
        // HTML-comment syntax that we've used historically, plus we likely want some way to coalesce both
        // marker types into a single thing for auto mode (the code below emits both separately for auto).
        // It may be better to use a custom element like <blazor-component ...>[prerendered]<blazor-component>
        // so it's easier for the JS code to react automatically whenever this gets inserted or updated during
        // streaming SSR or progressively-enhanced navigation.

        var (serverMarker, webAssemblyMarker) = componentState.Component is SSRRenderModeBoundary boundary
            ? boundary.ToMarkers(_httpContext)
            : default;

        if (serverMarker.HasValue)
        {
            if (!_httpContext.Response.HasStarted)
            {
                _httpContext.Response.Headers.CacheControl = "no-cache, no-store, max-age=0";
            }

            ServerComponentSerializer.AppendPreamble(output, serverMarker.Value);
        }

        if (webAssemblyMarker.HasValue)
        {
            WebAssemblyComponentSerializer.AppendPreamble(output, webAssemblyMarker.Value);
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

        if (webAssemblyMarker.HasValue && webAssemblyMarker.Value.PrerenderId is not null)
        {
            WebAssemblyComponentSerializer.AppendEpilogue(output, webAssemblyMarker.Value);
        }

        if (serverMarker.HasValue && serverMarker.Value.PrerenderId is not null)
        {
            ServerComponentSerializer.AppendEpilogue(output, serverMarker.Value);
        }
    }

    private readonly record struct ComponentIdAndDepth(int ComponentId, int Depth);
}
