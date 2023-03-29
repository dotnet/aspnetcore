// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
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

    internal static Task RenderComponentToResponse(
        HttpContext httpContext,
        Type componentType,
        IReadOnlyDictionary<string, object?>? componentParameters)
    {
        var renderer = httpContext.RequestServices.GetRequiredService<EndpointHtmlRenderer>();
        if (HttpMethods.IsPost(httpContext.Request.Method))
        {
            // We are receiving a form post.
            // 1. Check for a handler or use a default handler. By convention, the default handler is
            // identified by the request path.
            var handler = httpContext.Request.Path.Value;
            if (httpContext.Request.Query.TryGetValue("handler", out var value))
            {
                if (value.Count != 1)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return Task.CompletedTask;
                }
                else
                {
                    handler = value[0];
                }
            }

            renderer.NamedFormHandler = handler;

            var formState = (StaticFormStateProvider)httpContext.RequestServices.GetRequiredService<IFormStateProvider>();
            formState.SetFormState(httpContext.Request, handler);
        }

        return renderer.Dispatcher.InvokeAsync(async () =>
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

    private static TextWriter CreateResponseWriter(Stream bodyStream)
    {
        // Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
        const int DefaultBufferSize = 16 * 1024;
        return new HttpResponseStreamWriter(bodyStream, Encoding.UTF8, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
    }
}
