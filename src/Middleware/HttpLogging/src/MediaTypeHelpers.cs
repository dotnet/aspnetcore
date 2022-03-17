// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Net.Http.Headers;
using static Microsoft.AspNetCore.HttpLogging.MediaTypeOptions;

namespace Microsoft.AspNetCore.HttpLogging;

internal static class MediaTypeHelpers
{
    private static readonly List<Encoding> SupportedEncodings = new List<Encoding>()
        {
            Encoding.UTF8,
            Encoding.Unicode,
            Encoding.ASCII,
            Encoding.Latin1 // TODO allowed by default? Make this configurable?
        };

    public static bool TryGetEncodingForMediaType(string? contentType, List<MediaTypeState> mediaTypeList, [NotNullWhen(true)] out Encoding? encoding)
    {
        encoding = null;
        if (mediaTypeList == null || mediaTypeList.Count == 0 || string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
        {
            return false;
        }

        if (mediaType.Charset.HasValue)
        {
            // Create encoding based on charset
            var requestEncoding = mediaType.Encoding;

            if (requestEncoding != null)
            {
                for (var i = 0; i < SupportedEncodings.Count; i++)
                {
                    if (string.Equals(requestEncoding.WebName,
                        SupportedEncodings[i].WebName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        encoding = SupportedEncodings[i];
                        return true;
                    }
                }
            }
        }
        else
        {
            // TODO Binary format https://github.com/dotnet/aspnetcore/issues/31884
            foreach (var state in mediaTypeList)
            {
                var type = state.MediaTypeHeaderValue;
                if (type.MatchesMediaType(mediaType.MediaType))
                {
                    // We always set encoding
                    encoding = state.Encoding!;
                    return true;
                }
            }
        }

        return false;
    }
}
