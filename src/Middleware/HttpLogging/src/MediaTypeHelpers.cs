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

    // TODO Binary format https://github.com/dotnet/aspnetcore/issues/31884
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

        foreach (var state in mediaTypeList)
        {
            var type = state.MediaTypeHeaderValue;
            if (type.MatchesMediaType(mediaType.MediaType))
            {
                encoding = mediaType.Encoding;
                if (encoding == null)
                {
                    // No encoding specified, use the default.
                    encoding = state.Encoding!;
                    return true;
                }

                // Only allow specific encodings.
                for (var i = 0; i < SupportedEncodings.Count; i++)
                {
                    if (string.Equals(encoding.WebName,
                        SupportedEncodings[i].WebName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        encoding = SupportedEncodings[i];
                        return true;
                    }
                }

                break;
            }
        }

        return false;
    }
}
