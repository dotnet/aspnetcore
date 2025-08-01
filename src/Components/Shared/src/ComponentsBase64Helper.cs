// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Components;

internal static class ComponentsBase64Helper
{
#if USE_WORKAROUND
    private const string Base64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    private const char Base64Padding = '=';
    private const string Base64UrlChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
#endif

    public static string ToBase64(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

#if USE_WORKAROUND
        return EncodeToBase64(data);
#else
        return Convert.ToBase64String(data);
#endif
    }

    public static string ToBase64(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

#if USE_WORKAROUND
        return EncodeToBase64(data);
#else
        return Convert.ToBase64String(data);
#endif
    }

    public static string ToBase64(Span<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

#if USE_WORKAROUND
        return EncodeToBase64(data);
#else
        return Convert.ToBase64String(data);
#endif
    }

    public static string ToBase64Url(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

#if USE_WORKAROUND
        return EncodeToBase64Url(data);
#else
        return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(data);
#endif
    }

    public static string ToBase64Url(byte[] data)
    {
        if (data is null || data.Length == 0)
        {
            return string.Empty;
        }

#if USE_WORKAROUND
        return EncodeToBase64Url(data);
#else
        return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(data);
#endif
    }

    public static string ToBase64Url(Span<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

#if USE_WORKAROUND
        return EncodeToBase64Url(data);
#else
        return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(data);
#endif
    }

    public static int ToBase64Url(ReadOnlySpan<byte> data, Span<char> output)
    {
        if (data.IsEmpty)
        {
            return 0;
        }

#if USE_WORKAROUND
        return EncodeToBase64Url(data, output);
#else
        return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(data, output);
#endif
    }

    public static string Base64UrlEncode(byte[] data)
    {
        return ToBase64Url(data);
    }

    public static byte[] Base64UrlDecode(string text)
    {
#if USE_WORKAROUND
        return DecodeFromBase64Url(text);
#else
        return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(text);
#endif
    }

#if USE_WORKAROUND
    private static string EncodeToBase64(ReadOnlySpan<byte> data)
    {
        var outputLength = ((data.Length + 2) / 3) * 4;
        var result = new char[outputLength];
        var resultIndex = 0;

        for (var i = 0; i < data.Length; i += 3)
        {
            var b1 = data[i];
            var b2 = i + 1 < data.Length ? data[i + 1] : (byte)0;
            var b3 = i + 2 < data.Length ? data[i + 2] : (byte)0;

            // First 6 bits of b1
            result[resultIndex++] = Base64Chars[b1 >> 2];

            // Last 2 bits of b1 and first 4 bits of b2
            result[resultIndex++] = Base64Chars[((b1 & 0x03) << 4) | (b2 >> 4)];

            // Last 4 bits of b2 and first 2 bits of b3
            if (i + 1 < data.Length)
            {
                result[resultIndex++] = Base64Chars[((b2 & 0x0F) << 2) | (b3 >> 6)];
            }
            else
            {
                result[resultIndex++] = Base64Padding;
            }

            // Last 6 bits of b3
            if (i + 2 < data.Length)
            {
                result[resultIndex++] = Base64Chars[b3 & 0x3F];
            }
            else
            {
                result[resultIndex++] = Base64Padding;
            }
        }

        return new string(result);
    }

    private static string EncodeToBase64Url(ReadOnlySpan<byte> data)
    {
        var outputLength = ((data.Length + 2) / 3) * 4;
        var result = new char[outputLength];
        var resultIndex = 0;

        for (var i = 0; i < data.Length; i += 3)
        {
            var b1 = data[i];
            var b2 = i + 1 < data.Length ? data[i + 1] : (byte)0;
            var b3 = i + 2 < data.Length ? data[i + 2] : (byte)0;

            // First 6 bits of b1
            result[resultIndex++] = Base64UrlChars[b1 >> 2];

            // Last 2 bits of b1 and first 4 bits of b2
            result[resultIndex++] = Base64UrlChars[((b1 & 0x03) << 4) | (b2 >> 4)];

            // Last 4 bits of b2 and first 2 bits of b3
            if (i + 1 < data.Length)
            {
                result[resultIndex++] = Base64UrlChars[((b2 & 0x0F) << 2) | (b3 >> 6)];

                // Last 6 bits of b3
                if (i + 2 < data.Length)
                {
                    result[resultIndex++] = Base64UrlChars[b3 & 0x3F];
                }
            }
        }

        return new string(result, 0, resultIndex);
    }

    private static int EncodeToBase64Url(ReadOnlySpan<byte> data, Span<char> output)
    {
        var resultIndex = 0;

        for (var i = 0; i < data.Length; i += 3)
        {
            var b1 = data[i];
            var b2 = i + 1 < data.Length ? data[i + 1] : (byte)0;
            var b3 = i + 2 < data.Length ? data[i + 2] : (byte)0;

            // First 6 bits of b1
            output[resultIndex++] = Base64UrlChars[b1 >> 2];

            // Last 2 bits of b1 and first 4 bits of b2
            output[resultIndex++] = Base64UrlChars[((b1 & 0x03) << 4) | (b2 >> 4)];

            // Last 4 bits of b2 and first 2 bits of b3
            if (i + 1 < data.Length)
            {
                output[resultIndex++] = Base64UrlChars[((b2 & 0x0F) << 2) | (b3 >> 6)];

                // Last 6 bits of b3
                if (i + 2 < data.Length)
                {
                    output[resultIndex++] = Base64UrlChars[b3 & 0x3F];
                }
            }
        }

        return resultIndex;
    }

    private static byte[] DecodeFromBase64Url(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        // Convert Base64Url to Base64 by replacing characters and adding padding
        var base64 = text.Replace('-', '+').Replace('_', '/');

        // Add padding
        var padding = (4 - (base64.Length % 4)) % 4;
        if (padding > 0)
        {
            base64 += new string('=', padding);
        }

        return Convert.FromBase64String(base64);
    }
#endif
}
