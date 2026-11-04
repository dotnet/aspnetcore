// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

internal static class McpAppPolicy
{
    internal const string HtmlMimeType = "text/html;profile=mcp-app";

    internal static JsonElement Wire<T>(T value) => JsonSerializer.SerializeToElement(value, McpJsonUtilities.DefaultOptions);

    internal static JsonElement? Ui(JsonElement value)
    {
        if (!value.TryGetProperty("_meta", out var meta) || meta.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (meta.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("App metadata must be an object.");
        }
        if (!meta.TryGetProperty("ui", out var ui) || ui.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (ui.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("App UI metadata must be an object.");
        }
        return ui;
    }

    internal static string? ResourceUri(Tool tool)
    {
        var wire = Wire(tool);
        JsonElement resource;
        if (Ui(wire) is { } ui && ui.TryGetProperty("resourceUri", out resource))
        {
            return ValidateResourceUri(resource);
        }
        if (wire.TryGetProperty("_meta", out var meta) && meta.ValueKind == JsonValueKind.Object &&
            meta.TryGetProperty("ui/resourceUri", out resource))
        {
            return ValidateResourceUri(resource);
        }
        return null;
    }

    private static string? ValidateResourceUri(JsonElement resource)
    {
        if (resource.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (resource.ValueKind != JsonValueKind.String ||
            !resource.GetString()!.StartsWith("ui://", StringComparison.OrdinalIgnoreCase) ||
            !Uri.TryCreate(resource.GetString(), UriKind.Absolute, out var uri) || uri.Scheme != "ui")
        {
            throw new InvalidOperationException("App tools must reference an absolute ui:// resource.");
        }
        return resource.GetString();
    }

    internal static bool VisibleTo(Tool tool, string audience)
    {
        if (Ui(Wire(tool)) is not { } ui || !ui.TryGetProperty("visibility", out var visibility))
        {
            return true;
        }
        if (visibility.ValueKind != JsonValueKind.Array ||
            visibility.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String ||
                item.GetString() is not ("model" or "app")))
        {
            throw new InvalidOperationException("Tool UI visibility must contain only model or app.");
        }
        return visibility.EnumerateArray().Any(item => item.GetString() == audience);
    }

    internal static McpAppDocument ReadDocument(ReadResourceResult result, string uri, Resource? listing,
        McpContentOptions options)
    {
        var documents = result.Contents.Where(content => content.Uri == uri && content.MimeType == HtmlMimeType).ToArray();
        if (documents.Length != 1)
        {
            throw new InvalidOperationException("The app resource must contain exactly one matching HTML document.");
        }
        var selected = documents[0];
        var html = selected switch
        {
            TextResourceContents text => text.Text,
            BlobResourceContents blob when blob.Blob.Length <= ((long)options.MaximumDocumentBytes + 2) / 3 * 4 =>
                new UTF8Encoding(false, true).GetString(blob.DecodedData.Span),
            _ => throw new InvalidOperationException("The app document encoding or size is not supported.")
        };
        if (Encoding.UTF8.GetByteCount(html) > options.MaximumDocumentBytes)
        {
            throw new InvalidOperationException("The app document exceeds the configured byte limit.");
        }

        var metadata = MergeMetadata(listing is null ? null : Ui(Wire(listing)), Ui(Wire(selected)));
        var effective = new McpContentPolicy();
        if (metadata is { } ui)
        {
            if (ui.TryGetProperty("csp", out var csp))
            {
                if (csp.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException("The app CSP declaration must be an object.");
                }
                foreach (var property in csp.EnumerateObject())
                {
                    var (grants, destination) = property.Name switch
                    {
                        "resourceDomains" => (options.BrowserPolicy.ResourceDomains, effective.ResourceDomains),
                        "connectDomains" => (options.BrowserPolicy.ConnectDomains, effective.ConnectDomains),
                        "frameDomains" => (options.BrowserPolicy.FrameDomains, effective.FrameDomains),
                        "baseUriDomains" => (options.BrowserPolicy.BaseUriDomains, effective.BaseUriDomains),
                        _ => throw new InvalidOperationException($"Unsupported app CSP declaration: {property.Name}.")
                    };
                    if (property.Value.ValueKind != JsonValueKind.Array)
                    {
                        throw new InvalidOperationException("App CSP domains must be arrays.");
                    }
                    foreach (var domain in property.Value.EnumerateArray())
                    {
                        var requested = domain.ValueKind == JsonValueKind.String ? domain.GetString() : null;
                        if (!Uri.TryCreate(requested, UriKind.Absolute, out var parsed) ||
                            (parsed.Scheme is not ("http" or "https") &&
                                !(property.Name == "connectDomains" && parsed.Scheme is "ws" or "wss")) ||
                            parsed.GetLeftPart(UriPartial.Authority) != requested || !grants.Contains(requested))
                        {
                            throw new InvalidOperationException($"The app requested an ungranted browser origin: {requested}.");
                        }
                        destination.Add(requested);
                    }
                }
            }
            if (ui.TryGetProperty("permissions", out var permissions))
            {
                if (permissions.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException("App browser permissions must be an object.");
                }
                foreach (var permission in permissions.EnumerateObject())
                {
                    var feature = permission.Name switch
                    {
                        "camera" => "camera",
                        "microphone" => "microphone",
                        "geolocation" => "geolocation",
                        "clipboardWrite" => "clipboard-write",
                        _ => throw new InvalidOperationException($"Unsupported app browser permission: {permission.Name}.")
                    };
                    if (permission.Value.ValueKind != JsonValueKind.Object ||
                        permission.Value.EnumerateObject().Any() ||
                        !options.BrowserPolicy.Permissions.Contains(feature))
                    {
                        throw new InvalidOperationException($"The app requested an ungranted browser permission: {feature}.");
                    }
                    effective.Permissions.Add(feature);
                }
            }
        }
        return new(html, effective);
    }

    private static JsonElement? MergeMetadata(JsonElement? listing, JsonElement? content)
    {
        if (listing is null)
        {
            return content;
        }
        if (content is null)
        {
            return listing;
        }
        var merged = JsonNode.Parse(listing.Value.GetRawText())!.AsObject();
        foreach (var property in content.Value.EnumerateObject())
        {
            merged[property.Name] = JsonNode.Parse(property.Value.GetRawText());
        }
        return Wire(merged);
    }

    internal static object ProjectForModel(CallToolResult result)
    {
        var node = JsonNode.Parse(Wire(result).GetRawText())!.AsObject();
        RemoveMetadata(node);
        if (node["content"] is JsonArray wireContents)
        {
            foreach (var content in wireContents.OfType<JsonObject>())
            {
                RemoveMetadata(content);
                if (content["resource"] is JsonObject resource)
                {
                    RemoveMetadata(resource);
                }
            }
        }
        var projected = JsonSerializer.SerializeToElement(node, McpJsonUtilities.DefaultOptions);
        var modelResult = projected.Deserialize<CallToolResult>(McpJsonUtilities.DefaultOptions)!;
        if (modelResult.IsError is not true && modelResult.StructuredContent is null && modelResult.Meta is null)
        {
            if (modelResult.Content.Count == 1 && modelResult.Content[0].ToAIContent() is { } singleContent)
            {
                return singleContent;
            }
            if (modelResult.Content.Count > 1 &&
                modelResult.Content.Select(content => content.ToAIContent()).ToArray() is { } converted &&
                converted.All(content => content is not null))
            {
                return converted;
            }
        }
        return projected;
    }

    private static void RemoveMetadata(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            obj.Remove("_meta");
        }
    }
}
