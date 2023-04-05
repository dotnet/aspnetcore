// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    public async Task SendStreamingUpdatesAsync(HttpContext httpContext, Task untilTaskCompleted, TextWriter writer)
    {
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
            HandleExceptionAfterResponseStarted(httpContext, writer, ex);

            // The rest of the pipeline can treat this as a regular unhandled exception
            // TODO: Is this really right? I think we'll terminate the response in an invalid way.
            throw;
        }
    }

    private void SendBatchAsStreamingUpdate(in RenderBatch renderBatch)
    {
        var count = renderBatch.UpdatedComponents.Count;
        if (count > 0 && _streamingUpdatesWriter is { } writer)
        {
            writer.Write("<blazor-ssr>");

            // We deduplicate the set of components in the batch because we're sending their entire current rendered
            // state, not just an intermediate diff (so there's never a reason to include the same component output
            // more than once in this callback)
            var htmlComponentIds = new HashSet<int>(count);
            for (var i = 0; i < count; i++)
            {
                ref var diff = ref renderBatch.UpdatedComponents.Array[i];
                var componentId = diff.ComponentId;
                if (htmlComponentIds.Add(componentId))
                {
                    // This relies on the component producing well-formed markup (i.e., it can't have a closing
                    // </template> at the top level without a preceding matching <template>). Alternatively we
                    // could look at using a custom TextWriter that does some extra encoding of all the content
                    // as it is being written out.
                    writer.Write($"<template blazor-component-id=\"{componentId}\">");
                    WriteComponentHtml(componentId, writer);
                    writer.Write("</template>");
                }
            }

            writer.Write("</blazor-ssr>");
        }
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
}
