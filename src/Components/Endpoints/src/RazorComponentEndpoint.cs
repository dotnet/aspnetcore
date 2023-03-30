// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class RazorComponentEndpoint
{
    public static RequestDelegate CreateRouteDelegate(Type componentType)
    {
        return httpContext =>
            RenderComponentToResponse(httpContext, componentType, componentParameters: null);
    }

    internal static async Task RenderComponentToResponse(
        HttpContext httpContext,
        Type componentType,
        IReadOnlyDictionary<string, object?>? componentParameters)
    {
        var renderer = httpContext.RequestServices.GetRequiredService<EndpointHtmlRenderer>();
        var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();

        if (HttpMethods.IsPost(httpContext.Request.Method))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (Exception ex)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            ResolveFormHandler(httpContext, renderer);
        }
        else
        {
            // Set the antiforgery token on the request cookie.
            // We'll have to figure out what we can/want to do for streaming rendering
            // since the cookie needs to be set before the response starts.
            antiforgery.GetAndStoreTokens(httpContext);
        }

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            // We could pool these dictionary instances if we wanted, and possibly even the ParameterView
            // backing buffers could come from a pool like they do during rendering.
            var hostParameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(RazorComponentEndpointHost.RenderMode), RenderMode.Static },
                { nameof(RazorComponentEndpointHost.ComponentType), componentType },
                { nameof(RazorComponentEndpointHost.ComponentParameters), componentParameters },
            });

            await renderer.InitializeStandardComponentServicesAsync(httpContext);

            var htmlContent = await renderer.StaticComponentAsync(
                httpContext,
                typeof(RazorComponentEndpointHost),
                hostParameters);

            if (renderer.NamedFormHandler != null)
            {
                if (!renderer.HasSubmitEvent())
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
                else
                {
                    await renderer.DispatchSubmitEventAsync();
                }
            }

            await using var writer = CreateResponseWriter(httpContext.Response.Body);
            await htmlContent.WriteToAsync(writer);

            // Perf: Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
            // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
            // response as part of the Dispose which has a perf impact.
            await writer.FlushAsync();
        });
    }

    private static void ResolveFormHandler(HttpContext httpContext, EndpointHtmlRenderer renderer)
    {
        var handler = httpContext.Request.Path.Value;
        if (httpContext.Request.Query.TryGetValue("handler", out var value))
        {
            if (value.Count != 1)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            else
            {
                handler = value[0];
            }
        }

        renderer.NamedFormHandler = handler;
    }

    private static TextWriter CreateResponseWriter(Stream bodyStream)
    {
        // Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
        const int DefaultBufferSize = 16 * 1024;
        return new HttpResponseStreamWriter(bodyStream, Encoding.UTF8, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
    }
}
