// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

internal class PassiveComponentRenderer
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IViewBufferScope _viewBufferScope;
    private readonly IHttpResponseStreamWriterFactory _writerFactory;
    private readonly HtmlEncoder _htmlEncoder;

    public PassiveComponentRenderer(
        ILoggerFactory loggerFactory,
        IViewBufferScope viewBufferScope,
        IHttpResponseStreamWriterFactory writerFactory,
        HtmlEncoder htmlEncoder)
    {
        _loggerFactory = loggerFactory;
        _viewBufferScope = viewBufferScope;
        _writerFactory = writerFactory;
        _htmlEncoder = htmlEncoder;
    }

    public Task HandleRequest(HttpContext httpContext, ComponentRenderMode renderMode, IComponent componentInstance, IReadOnlyDictionary<string, object?>? parameters)
    {
        return HandleRequest(httpContext, renderMode, componentInstance, componentInstance.GetType(), parameters);
    }

    public Task HandleRequest(HttpContext httpContext, ComponentRenderMode renderMode, Type componentType, IReadOnlyDictionary<string, object?>? parameters)
    {
        return HandleRequest(httpContext, renderMode, null, componentType, parameters);
    }

    private async Task HandleRequest(HttpContext httpContext, ComponentRenderMode renderMode, IComponent? componentInstance, Type componentType, IReadOnlyDictionary<string, object?>? parameters)
    {
        using var htmlRenderer = componentInstance is null
            ? new HtmlRenderer(httpContext.RequestServices, _loggerFactory, _viewBufferScope)
            : new HtmlRenderer(httpContext.RequestServices, _loggerFactory, _viewBufferScope, new PassiveComponentInstanceActivator(httpContext.RequestServices, componentInstance));
        var staticComponentRenderer = new StaticComponentRenderer(htmlRenderer);

        var routeData = httpContext.GetRouteData();
        var rootComponentType = typeof(RouteView);
        var combinedParametersDict = GetCombinedParameters(routeData, parameters);
        var formValues = httpContext.Request.HasFormContentType ? httpContext.Request.Form : null;
        var rootComponentParameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(RouteView.RouteData), new Components.RouteData(componentType, combinedParametersDict!) { FormValues = formValues } },
            { nameof(RouteView.RenderMode), renderMode },
        });

        var result = await staticComponentRenderer.PrerenderComponentAsync(
            rootComponentParameters,
            httpContext,
            rootComponentType,
            awaitQuiescence: false);

        var viewBuffer = new ViewBuffer(_viewBufferScope, nameof(RazorComponentsEndpointRouteBuilderExtensions), ViewBuffer.ViewPageSize);
        viewBuffer.AppendHtml(result);

        using var writer = _writerFactory.CreateWriter(httpContext.Response.BodyWriter.AsStream(), Encoding.UTF8);
        await viewBuffer.WriteToAsync(writer, _htmlEncoder);

        try
        {
            if (htmlRenderer.StreamingRenderBatches is not null)
            {
                await foreach (var batch in htmlRenderer.StreamingRenderBatches.ReadAllAsync(httpContext.RequestAborted))
                {
                    // TODO: Instead of passing 'result', we should only pass 'batch', and WriteDiffAsync should
                    // render that batch to the output instead of the whole page
                    await WriteDiffAsync(httpContext, result);
                }
            }
        }
        catch (AggregateException ex) when (ex.InnerException is NavigationException navException)
        {
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.Redirect(navException.Location);
            }
            else
            {
                // During streaming rendering, if there's a navigation, we have to translate that into script
                await writer.WriteAsync("<script>location.href = ");
                await writer.WriteAsync(JsonSerializer.Serialize(navException.Location));
                await writer.WriteAsync("</script>");
            }
            return;
        }

        // Finally, emit any persisted state
        var persistenceManager = httpContext.RequestServices.GetService<ComponentStatePersistenceManager>();
        if (persistenceManager is not null)
        {
            // In a real implementation, we need to solve some problems here:
            // [1] We shouldn't be persisting everything twice (once for server, once for WebAssembly)
            //     Instead, the persistence mechanism needs to know which components use Server rendermode
            //     and which ones use WebAssembly rendermode, and only persist states for those ones,
            //     and store the states in separate collections. It also has to understand the component
            //     hierarchy so it knows that descendants of interactive components are also interactive
            //     and take the same rendermode by default.
            // [2] We have to think through what this means for streaming rendering. Do we serialize out
            //     the state on every renderbatch? Presumably not! But then how do we know when to serialize
            //     the state? If the rule is "things only become interactive after the streaming SSR process
            //     has completed" then it's easy enough; just handle things like below.
            var store = new PrerenderComponentApplicationStore();
            var hasState = await persistenceManager.PersistStateAsync(store, htmlRenderer);
            if (hasState)
            {
                await writer.WriteAsync("\n<!--Blazor-Component-State-WebAssembly:");
                await writer.WriteAsync(store.PersistedState);
                await writer.WriteAsync("-->");
            }

            var dataProtection = httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>();
            var protectedStore = new ProtectedPrerenderComponentApplicationStore(dataProtection);
            var hasProtectedState = await persistenceManager.PersistStateAsync(protectedStore, htmlRenderer);
            if (hasProtectedState)
            {
                await writer.WriteAsync("\n<!--Blazor-Component-State-Server:");
                await writer.WriteAsync(protectedStore.PersistedState);
                await writer.WriteAsync("-->");
            }
        }
    }

    private static Dictionary<string, object?> GetCombinedParameters(AspNetCore.Routing.RouteData routeData, IReadOnlyDictionary<string, object?>? explicitParameters)
    {
        var result = new Dictionary<string, object?>();

        foreach (var kvp in routeData.Values)
        {
            result.Add(kvp.Key, kvp.Value);
        }

        if (explicitParameters is not null)
        {
            foreach (var kvp in explicitParameters)
            {
                result.Add(kvp.Key, kvp.Value);
            }
        }

        return result;
    }

    private async Task WriteDiffAsync(HttpContext httpContext, IHtmlContent result)
    {
        // TODO: Instead of re-rendering the entire page as HTML, just emit the edits in this batch
        // and have client-side JS apply it to the existing DOM.

        var viewBuffer = new ViewBuffer(_viewBufferScope, nameof(RazorComponentsEndpointRouteBuilderExtensions), ViewBuffer.ViewPageSize);
        viewBuffer.AppendHtml(result);

        // Convert to a JSON string. This demo implementation is very unrealistic. A real implementation
        // would not do anything like this.
        using var memoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(memoryStream);
        await viewBuffer.WriteToAsync(streamWriter, _htmlEncoder);
        await streamWriter.FlushAsync();
        memoryStream.Position = 0;
        using var streamReader = new StreamReader(memoryStream);
        var htmlString = streamReader.ReadToEnd();
        var htmlStringJson = JsonSerializer.Serialize(htmlString);

        using var writer = _writerFactory.CreateWriter(httpContext.Response.BodyWriter.AsStream(), Encoding.UTF8);
        await writer.WriteAsync("\n<script>(function() { const newHtml = ");
        await writer.WriteAsync(htmlStringJson);
        await writer.WriteAsync("; document.body.innerHTML = new DOMParser().parseFromString(newHtml, 'text/html').querySelector('body').innerHTML;");
        await writer.WriteAsync("})()</script>");
    }
}
