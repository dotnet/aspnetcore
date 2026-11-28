// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

internal static class AntiforgeryTokenEndpoint
{
    internal const string RequestHeaderName = "Blazor-Antiforgery";

    private static bool IsEnabled(HttpContext context) =>
        context.Items.ContainsKey(MiddlewareInvokedKeys.Antiforgery) &&
        context.RequestServices.GetService<AntiforgeryStateProvider>() is EndpointAntiforgeryStateProvider;

    internal static void WriteConfiguration(HttpContext context, TextWriter writer)
    {
        if (context.Response.StatusCode is >= 300 and < 400)
        {
            return;
        }

        var configuration = JsonSerializer.Serialize(
            IsEnabled(context) ? context.Request.GetEncodedPathAndQuery() : null, AntiforgeryTokenJsonContext.Default.String);
        writer.Write("<!--Blazor-Antiforgery:");
        writer.Write(Convert.ToBase64String(Encoding.UTF8.GetBytes(configuration)));
        writer.Write("-->");
    }

    internal static async Task WriteToken(HttpContext context)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AntiforgeryTokenEndpoint));
        if (!IsEnabled(context))
        {
            logger.LogDebug("Token acquisition requires the endpoint antiforgery provider and middleware.");
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        if (context.Request.Headers[RequestHeaderName] != "true")
        {
            logger.LogDebug("Token acquisition requires the Blazor-Antiforgery request header.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var tokens = context.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);
        if (tokens.RequestToken is null)
        {
            throw new InvalidOperationException("The antiforgery service did not provide a request token.");
        }

        await context.Response.WriteAsJsonAsync(
            new AntiforgeryRequestToken(tokens.RequestToken, tokens.FormFieldName),
            AntiforgeryTokenJsonContext.Default.AntiforgeryRequestToken,
            cancellationToken: context.RequestAborted);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AntiforgeryRequestToken))]
[JsonSerializable(typeof(string))]
internal partial class AntiforgeryTokenJsonContext : JsonSerializerContext
{
}
