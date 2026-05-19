// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Rendering;

internal sealed class SystemTextJsonHelper : IJsonHelper
{
    private readonly JsonSerializerOptions _htmlSafeJsonSerializerOptions;

    public SystemTextJsonHelper(IOptions<JsonOptions> options)
    {
        _htmlSafeJsonSerializerOptions = GetHtmlSafeSerializerOptions(options.Value.JsonSerializerOptions);
    }

    /// <inheritdoc />
    public IHtmlContent Serialize(object value)
    {
        // JsonSerializer always encodes non-ASCII chars, so we do not need
        // to do anything special with the SerializerOptions
        var json = JsonSerializer.Serialize(value, _htmlSafeJsonSerializerOptions);
        return new HtmlString(json);
    }

    private static JsonSerializerOptions GetHtmlSafeSerializerOptions(JsonSerializerOptions serializerOptions)
    {
        if (serializerOptions.Encoder is null || serializerOptions.Encoder == JavaScriptEncoder.Default)
        {
            return serializerOptions;
        }

        return new JsonSerializerOptions(serializerOptions)
        {
            Encoder = JavaScriptEncoder.Default,
        };
    }
}
