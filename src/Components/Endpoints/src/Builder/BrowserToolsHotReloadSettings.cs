// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class BrowserToolsHotReloadSettings
{
    internal const string Path = "/_framework/dotnet-browser-tools/hot-reload-settings.json";

    private const string DotNetWatchEnvironmentVariable = "DOTNET_WATCH";
    private const string DisabledResponse = """{ "hotReload": false }""";

    public static EndpointBuilder? GetEndpoint(IServiceProvider services)
    {
        var environment = services.GetService<IWebHostEnvironment>();
        var configuration = services.GetService<IConfiguration>();

        if (environment?.IsDevelopment() != true ||
            string.Equals(configuration?[DotNetWatchEnvironmentVariable], "1", StringComparison.Ordinal))
        {
            return null;
        }

        var builder = new RouteEndpointBuilder(
            WriteResponse,
            RoutePatternFactory.Parse(Path),
            int.MaxValue)
        {
            DisplayName = "Browser tools Hot Reload settings",
        };

        builder.Metadata.Add(new HttpMethodMetadata([HttpMethods.Get]));

        return builder;
    }

    private static Task WriteResponse(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";

        return context.Response.WriteAsync(DisabledResponse, context.RequestAborted);
    }
}
