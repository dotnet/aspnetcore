// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components.Mcp.Hosting;
using Microsoft.AspNetCore.Components.Mcp.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.AspNetCore.Components.Mcp.Protocol;

internal sealed class McpComponentInvoker
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly McpComponentApplication _application;
    private readonly McpComponentHost _host;
    private readonly IServiceScopeFactory _scopeFactory;

    public McpComponentInvoker(McpComponentApplication application, McpComponentHost host, IServiceScopeFactory scopeFactory)
    {
        _application = application;
        _host = host;
        _scopeFactory = scopeFactory;
    }

    public async ValueTask<CallToolResult> DispatchAsync(
        McpComponentApplication.View viewState,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var principal = request.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        var arguments = request.Params!.Arguments!;
        try
        {
            var method = arguments["method"].GetString();
            if (method == "CLOSE")
            {
                await _application.RemoveViewAsync(viewState);
                return new CallToolResult { Content = [new TextContentBlock { Text = "Component view closed." }] };
            }

            if (method is not ("GET" or "POST")
                || !arguments.TryGetValue("location", out var locationElement)
                || locationElement.ValueKind != JsonValueKind.String
                || !Uri.TryCreate(locationElement.GetString(), UriKind.Absolute, out var location)
                || !_application.BaseUri.IsBaseOf(location))
            {
                throw new McpException("The component navigation must identify a location within the registered application.");
            }

            var enctype = "application/x-www-form-urlencoded";
            if (arguments.TryGetValue("enctype", out var enctypeElement))
            {
                if (enctypeElement.ValueKind != JsonValueKind.String)
                {
                    throw new McpException("The form encoding must be a string.");
                }

                enctype = enctypeElement.GetString();
            }

            if (method == "POST" && enctype is not ("application/x-www-form-urlencoded" or "multipart/form-data"))
            {
                throw new McpException("Only URL-encoded and multipart component forms are supported.");
            }

            var fields = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            if (arguments.TryGetValue("fields", out var fieldsElement) && fieldsElement.ValueKind != JsonValueKind.Null)
            {
                if (fieldsElement.ValueKind != JsonValueKind.Object)
                {
                    throw new McpException("Form fields must be an object containing arrays of strings.");
                }

                foreach (var field in fieldsElement.EnumerateObject())
                {
                    if (field.Value.ValueKind != JsonValueKind.Array
                        || field.Value.EnumerateArray().Any(value => value.ValueKind != JsonValueKind.String))
                    {
                        throw new McpException("Form fields must be arrays of strings.");
                    }

                    var values = new StringValues(field.Value.EnumerateArray().Select(value => value.GetString()).ToArray());
                    fields[field.Name] = fields.TryGetValue(field.Name, out var existing) ? StringValues.Concat(existing, values) : values;
                }
            }

            string? handler = null;
            if (method == "POST")
            {
                if (!fields.TryGetValue("_handler", out var handlers) || handlers.Count != 1 || string.IsNullOrEmpty(handlers[0]))
                {
                    throw new McpException("The form submission must identify exactly one named form handler.");
                }

                handler = handlers[0];
            }

            using var submission = ReadFiles(arguments);
            if (method == "GET" && (fields.Count != 0 || submission.Files.Count != 0))
            {
                throw new McpException("GET component navigation cannot contain form data.");
            }

            if (enctype != "multipart/form-data" && submission.Files.Count != 0)
            {
                throw new McpException("File contents require a multipart component form.");
            }

            var rendered = await RenderFollowingRedirectsAsync(new McpComponentRequest(location, principal, handler, fields, submission.Files),
                viewState.Parameters, cancellationToken);
            return CreateResult(viewState, rendered.Result, rendered.Location, rendered.Redirected);
        }
        catch (McpFormSubmissionException exception)
        {
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = exception.Message }] };
        }
    }

    public async ValueTask<CallToolResult> OpenFromToolAsync(
        RequestContext<CallToolRequestParams> request,
        CallToolResult? openingResult,
        CancellationToken cancellationToken)
    {
        var response = openingResult is null ? null : JsonSerializer.SerializeToElement(openingResult, McpJsonUtilities.DefaultOptions)
            .Deserialize<CallToolResult>(McpJsonUtilities.DefaultOptions)
                ?? throw new InvalidOperationException("The opening tool result could not be copied.");
        var parameters = response is null
            ? McpComponentParameters.Empty
            : McpComponentParameters.Capture(_application.InitialParametersFactory!(response));
        if (response?.Meta?.ContainsKey("blazor") == true)
        {
            throw new InvalidOperationException("The opening tool result already contains reserved Blazor app metadata.");
        }

        var principal = request.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        var rendered = await RenderFollowingRedirectsAsync(
            new McpComponentRequest(_application.InitialLocation, principal), parameters, cancellationToken);
        var view = await _application.CreateViewAsync(principal, parameters);
        return CreateResult(view, rendered.Result, rendered.Location, rendered.Redirected, response);
    }

    private async Task<(McpRenderResult Result, Uri Location, bool Redirected)> RenderFollowingRedirectsAsync(
        McpComponentRequest request, McpComponentParameters parameters, CancellationToken cancellationToken)
    {
        var redirected = false;
        var replaceHistory = false;
        for (var count = 0; count < 10; count++)
        {
            McpRenderResult result;
            if (count == 0)
            {
                result = await _host.RenderAsync(_application, request, parameters, cancellationToken);
            }
            else
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var host = scope.ServiceProvider.GetRequiredService<McpComponentHost>();
                result = await host.RenderAsync(_application, request, parameters, cancellationToken);
            }
            if (result.Redirect is not { } target)
            {
                return (result with { ReplaceHistory = replaceHistory }, request.Location, redirected);
            }

            if (!_application.BaseUri.IsBaseOf(target))
            {
                throw new McpException($"The component application requested external navigation to '{target}'.");
            }

            redirected = true;
            replaceHistory |= result.ReplaceHistory;
            request = new McpComponentRequest(target, request.User);
        }

        throw new McpException("The component application exceeded the redirect limit.");
    }

    private CallToolResult CreateResult(McpComponentApplication.View view, McpRenderResult result, Uri location, bool redirected, CallToolResult? openingResult = null)
    {
        if (result.Redirect is not null)
        {
            throw new McpException($"The component application requested navigation to '{result.Redirect}'.");
        }

        var response = openingResult ?? new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"Rendered {_application.Name} at {location}." }],
        };
        response.Meta ??= new JsonObject();
        if (response.Meta.ContainsKey("blazor"))
        {
            throw new InvalidOperationException("The opening tool result already contains reserved Blazor app metadata.");
        }

        response.Meta["blazor"] = new JsonObject
        {
            ["viewId"] = view.Id.ToString(),
            ["location"] = location.AbsoluteUri,
            ["baseUri"] = _application.BaseUri.AbsoluteUri,
            ["dispatchTool"] = _application.DispatchToolName,
            ["document"] = result.Html,
            ["browserOptions"] = new JsonObject
            {
                ["preserveDom"] = _application.PreserveDom,
                ["logLevel"] = (int)_application.LogLevel,
            },
            ["redirected"] = redirected,
            ["replaceHistory"] = result.ReplaceHistory,
        };
        return response;
    }

    private static SubmissionFiles ReadFiles(IDictionary<string, JsonElement> arguments)
    {
        var result = new SubmissionFiles();
        try
        {
            if (!arguments.TryGetValue("files", out var files) || files.ValueKind == JsonValueKind.Null)
            {
                return result;
            }

            if (files.ValueKind != JsonValueKind.Array)
            {
                throw new McpException("Submitted files must be an array.");
            }

            long totalBytes = 0;
            foreach (var item in files.EnumerateArray())
            {
                var file = item.Deserialize<SubmittedFile>(JsonOptions) ?? throw new McpException("A submitted file is invalid.");
                if (file.Name is null || file.FileName is null || file.ContentType is null || file.Data is null
                    || file.Data.Length > 48 * 1024 * 1024)
                {
                    throw new McpException("A submitted file is invalid or exceeds the submission limit.");
                }

                var bytes = new byte[file.Data.Length / 4 * 3 + 3];
                if (!Convert.TryFromBase64String(file.Data, bytes, out var length))
                {
                    throw new McpException("A submitted file contains invalid base64 data.");
                }

                totalBytes += length;
                if (totalBytes > 32 * 1024 * 1024)
                {
                    throw new McpException("Submitted files exceed the 32 MiB submission limit.");
                }

                if (length == 0 && file.FileName.Length == 0)
                {
                    continue;
                }

                var stream = new MemoryStream(bytes, 0, length, writable: false);
                result.Streams.Add(stream);
                var formFile = new FormFile(stream, 0, length, file.Name, file.FileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = file.ContentType,
                };
                formFile.Headers.LastModified = DateTimeOffset.FromUnixTimeMilliseconds(file.LastModified).ToString("R", CultureInfo.InvariantCulture);
                result.Files.Add(formFile);
            }

            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    private sealed class SubmissionFiles : IDisposable
    {
        public FormFileCollection Files { get; } = new();
        public List<MemoryStream> Streams { get; } = new();

        public void Dispose()
        {
            foreach (var stream in Streams)
            {
                stream.Dispose();
            }
        }
    }

    private sealed class SubmittedFile
    {
        public string? Name { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long LastModified { get; set; }
        public string? Data { get; set; }
    }
}
