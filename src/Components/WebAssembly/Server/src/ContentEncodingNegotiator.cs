// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

internal sealed class ContentEncodingNegotiator
{
    // List of encodings by preference order with their associated extension so that we can easily handle "*".
    private static readonly StringSegment[] _preferredEncodings =
        new StringSegment[] { "br", "gzip" };

    private static readonly Dictionary<StringSegment, string> _encodingExtensionMap = new Dictionary<StringSegment, string>(StringSegmentComparer.OrdinalIgnoreCase)
    {
        ["br"] = ".br",
        ["gzip"] = ".gz"
    };

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ContentEncodingNegotiator(RequestDelegate next, IWebHostEnvironment webHostEnvironment)
    {
        _next = next;
        _webHostEnvironment = webHostEnvironment;
    }

    public Task InvokeAsync(HttpContext context)
    {
        NegotiateEncoding(context);
        return _next(context);
    }

    private void NegotiateEncoding(HttpContext context)
    {
        var accept = context.Request.Headers.AcceptEncoding;

        if (StringValues.IsNullOrEmpty(accept))
        {
            return;
        }

        if (!StringWithQualityHeaderValue.TryParseList(accept, out var encodings) || encodings.Count == 0)
        {
            return;
        }

        var selectedEncoding = StringSegment.Empty;
        var selectedEncodingQuality = .0;

        foreach (var encoding in encodings)
        {
            var encodingName = encoding.Value;
            var quality = encoding.Quality.GetValueOrDefault(1);

            if (quality >= double.Epsilon && quality >= selectedEncodingQuality)
            {
                if (quality == selectedEncodingQuality)
                {
                    selectedEncoding = PickPreferredEncoding(context, selectedEncoding, encoding);
                }
                else if (_encodingExtensionMap.TryGetValue(encodingName, out var encodingExtension) && ResourceExists(context, encodingExtension))
                {
                    selectedEncoding = encodingName;
                    selectedEncodingQuality = quality;
                }

                if (StringSegment.Equals("*", encodingName, StringComparison.Ordinal))
                {
                    // If we *, pick the first preferrent encoding for which a resource exists.
                    selectedEncoding = PickPreferredEncoding(context, default, encoding);
                    selectedEncodingQuality = quality;
                }

                if (StringSegment.Equals("identity", encodingName, StringComparison.OrdinalIgnoreCase))
                {
                    selectedEncoding = StringSegment.Empty;
                    selectedEncodingQuality = quality;
                }
            }
        }

        if (_encodingExtensionMap.TryGetValue(selectedEncoding, out var extension))
        {
            context.Request.Path = context.Request.Path + extension;
            context.Response.Headers.ContentEncoding = selectedEncoding.Value;
            context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.ContentEncoding);
        }

        return;

        StringSegment PickPreferredEncoding(HttpContext context, StringSegment selectedEncoding, StringWithQualityHeaderValue encoding)
        {
            foreach (var preferredEncoding in _preferredEncodings)
            {
                if (preferredEncoding == selectedEncoding)
                {
                    return selectedEncoding;
                }

                if ((preferredEncoding == encoding.Value || encoding.Value == "*") && ResourceExists(context, _encodingExtensionMap[preferredEncoding]))
                {
                    return preferredEncoding;
                }
            }

            return StringSegment.Empty;
        }
    }

    private bool ResourceExists(HttpContext context, string extension) =>
        _webHostEnvironment.WebRootFileProvider.GetFileInfo(context.Request.Path + extension).Exists;
}
