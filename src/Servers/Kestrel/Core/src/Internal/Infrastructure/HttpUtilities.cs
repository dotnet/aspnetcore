// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal static partial class HttpUtilities
{
    public const string HttpUriScheme = "http://";
    public const string HttpsUriScheme = "https://";

    // readonly primitive statics can be Jit'd to consts https://github.com/dotnet/coreclr/issues/1079
    private static readonly ulong _httpSchemeLong = GetAsciiStringAsLong(HttpUriScheme + "\0");
    private static readonly ulong _httpsSchemeLong = GetAsciiStringAsLong(HttpsUriScheme);

    private const uint _httpGetMethodInt = 542393671; // GetAsciiStringAsInt("GET "); const results in better codegen

    private const ulong _http10VersionLong = 3471766442030158920; // GetAsciiStringAsLong("HTTP/1.0"); const results in better codegen
    private const ulong _http11VersionLong = 3543824036068086856; // GetAsciiStringAsLong("HTTP/1.1"); const results in better codegen

    private static readonly UTF8Encoding DefaultRequestHeaderEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly SpanAction<char, IntPtr> s_getHeaderName = GetHeaderName;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetKnownMethod(ulong mask, ulong knownMethodUlong, HttpMethod knownMethod, int length)
    {
        _knownMethods[GetKnownMethodIndex(knownMethodUlong)] = new Tuple<ulong, ulong, HttpMethod, int>(mask, knownMethodUlong, knownMethod, length);
    }

    private static void FillKnownMethodsGaps()
    {
        var knownMethods = _knownMethods;
        var length = knownMethods.Length;
        var invalidHttpMethod = new Tuple<ulong, ulong, HttpMethod, int>(_mask8Chars, 0ul, HttpMethod.Custom, 0);
        for (int i = 0; i < length; i++)
        {
            if (knownMethods[i] == null)
            {
                knownMethods[i] = invalidHttpMethod;
            }
        }
    }

    private static ulong GetAsciiStringAsLong(string str)
    {
        Debug.Assert(str.Length == 8, "String must be exactly 8 (ASCII) characters long.");

        Span<byte> bytes = stackalloc byte[8];
        OperationStatus operationStatus = Ascii.FromUtf16(str, bytes, out _);

        Debug.Assert(operationStatus == OperationStatus.Done);

        return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }

    private static uint GetAsciiStringAsInt(string str)
    {
        Debug.Assert(str.Length == 4, "String must be exactly 4 (ASCII) characters long.");

        Span<byte> bytes = stackalloc byte[4];
        OperationStatus operationStatus = Ascii.FromUtf16(str, bytes, out _);

        Debug.Assert(operationStatus == OperationStatus.Done);

        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    private static ulong GetMaskAsLong(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(bytes.Length == 8, "Mask must be exactly 8 bytes long.");

        return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }

    // The same as GetAsciiStringNonNullCharacters but throws BadRequest
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe string GetHeaderName(this ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty)
        {
            return string.Empty;
        }

        fixed (byte* source = &MemoryMarshal.GetReference(span))
        {
            return string.Create(span.Length, new IntPtr(source), s_getHeaderName);
        }
    }

    private static unsafe void GetHeaderName(Span<char> buffer, IntPtr state)
    {
        fixed (char* output = &MemoryMarshal.GetReference(buffer))
        {
            // This version of AsciiUtilities returns null if there are any null (0 byte) characters
            // in the string
            if (!StringUtilities.TryGetAsciiString((byte*)state.ToPointer(), output, buffer.Length))
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidCharactersInHeaderName);
            }
        }
    }

    public static string GetAsciiStringNonNullCharacters(this Span<byte> span)
        => StringUtilities.GetAsciiStringNonNullCharacters(span);

    public static string GetAsciiOrUTF8StringNonNullCharacters(this ReadOnlySpan<byte> span)
        => StringUtilities.GetAsciiOrUTF8StringNonNullCharacters(span, DefaultRequestHeaderEncoding);

    public static string GetRequestHeaderString(this ReadOnlySpan<byte> span, string name, Func<string, Encoding?> encodingSelector, bool checkForNewlineChars)
    {
        string result;
        if (ReferenceEquals(KestrelServerOptions.DefaultHeaderEncodingSelector, encodingSelector))
        {
            result = span.GetAsciiOrUTF8StringNonNullCharacters(DefaultRequestHeaderEncoding);
        }
        else
        {
            result = span.GetRequestHeaderStringWithoutDefaultEncodingCore(name, encodingSelector);
        }

        // New Line characters (CR, LF) are considered invalid at this point.
        if (checkForNewlineChars && ((ReadOnlySpan<char>)result).IndexOfAny('\r', '\n') >= 0)
        {
            throw new InvalidOperationException("Newline characters (CR/LF) are not allowed in request headers.");
        }

        return result;
    }

    private static string GetRequestHeaderStringWithoutDefaultEncodingCore(this ReadOnlySpan<byte> span, string name, Func<string, Encoding?> encodingSelector)
    {
        var encoding = encodingSelector(name);

        if (encoding is null)
        {
            return span.GetAsciiOrUTF8StringNonNullCharacters(DefaultRequestHeaderEncoding);
        }
        if (ReferenceEquals(encoding, Encoding.Latin1))
        {
            return span.GetLatin1StringNonNullCharacters();
        }
        try
        {
            return encoding.GetString(span);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    public static string GetAsciiStringEscaped(this ReadOnlySpan<byte> span, int maxChars)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < Math.Min(span.Length, maxChars); i++)
        {
            var ch = span[i];
            sb.Append(ch < 0x20 || ch >= 0x7F ? $"\\x{ch:X2}" : ((char)ch).ToString());
        }

        if (span.Length > maxChars)
        {
            sb.Append("...");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Checks that up to 8 bytes from <paramref name="span"/> correspond to a known HTTP method.
    /// </summary>
    /// <remarks>
    /// A "known HTTP method" can be an HTTP method name defined in the HTTP/1.1 RFC.
    /// Since all of those fit in at most 8 bytes, they can be optimally looked up by reading those bytes as a long. Once
    /// in that format, it can be checked against the known method.
    /// The Known Methods (CONNECT, DELETE, GET, HEAD, PATCH, POST, PUT, OPTIONS, TRACE) are all less than 8 bytes
    /// and will be compared with the required space. A mask is used if the Known method is less than 8 bytes.
    /// To optimize performance the GET method will be checked first.
    /// </remarks>
    /// <returns><c>true</c> if the input matches a known string, <c>false</c> otherwise.</returns>
    public static bool GetKnownMethod(this ReadOnlySpan<byte> span, out HttpMethod method, out int length)
    {
        method = GetKnownMethod(span, out length);
        return method != HttpMethod.Custom;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HttpMethod GetKnownMethod(this ReadOnlySpan<byte> span, out int methodLength)
    {
        methodLength = 0;
        if (sizeof(uint) <= span.Length)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(span) == _httpGetMethodInt)
            {
                methodLength = 3;
                return HttpMethod.Get;
            }
            else if (sizeof(ulong) <= span.Length)
            {
                var value = BinaryPrimitives.ReadUInt64LittleEndian(span);
                var index = GetKnownMethodIndex(value);
                var knownMethods = _knownMethods;
                if ((uint)index < (uint)knownMethods.Length)
                {
                    var knownMethod = _knownMethods[index];

                    if (knownMethod != null && (value & knownMethod.Item1) == knownMethod.Item2)
                    {
                        methodLength = knownMethod.Item4;
                        return knownMethod.Item3;
                    }
                }
            }
        }

        return HttpMethod.Custom;
    }

    /// <summary>
    /// Parses string <paramref name="value"/> for a known HTTP method.
    /// </summary>
    /// <remarks>
    /// A "known HTTP method" can be an HTTP method name defined in the HTTP/1.1 RFC.
    /// The Known Methods (CONNECT, DELETE, GET, HEAD, PATCH, POST, PUT, OPTIONS, TRACE)
    /// </remarks>
    /// <returns><see cref="HttpMethod"/></returns>
    public static HttpMethod GetKnownMethod(string? value)
    {
        // A perfect hash is used to get an index into a lookup-table for know HTTP-methods.
        //
        // If value is not null or an empty string, then local function PerfectHash is called.
        // This perfect hashing is done by lookup up 'associatedValues' from a pre-generated lookup-table.
        // The generation of that table was done by GNU gperf tool.
        // Once we have that perfect hash we use another lookup-table to get the know HTTP-method if found
        // or return HttpMethod.Custom if not found.
        // 
        // Further info and how to call gperf see https://github.com/dotnet/aspnetcore/pull/44096
        //
        // Code here could be removed if Roslyn improvements from
        // https://github.com/dotnet/roslyn/issues/56374 are added.

        const int MinWordLength = 3;
        const int MaxWordLength = 7;
        const int MaxHashValue = 12;

        if (string.IsNullOrEmpty(value))
        {
            return HttpMethod.None;
        }

        if ((uint)(value.Length - MinWordLength) <= (MaxWordLength - MinWordLength))
        {
            var methodsLookup = Methods();

            Debug.Assert(WordListForPerfectHashOfMethods.Length == (MaxHashValue + 1) && methodsLookup.Length == (MaxHashValue + 1));

            var index = PerfectHash(value);

            if (index < (uint)WordListForPerfectHashOfMethods.Length
                && WordListForPerfectHashOfMethods[index] == value
                && index < (uint)methodsLookup.Length)
            {
                return methodsLookup[(int)index];
            }
        }

        return HttpMethod.Custom;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint PerfectHash(ReadOnlySpan<char> str)
        {
            ReadOnlySpan<byte> associatedValues =
            [
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13,  5,  0, 13,
                13,  0,  0, 13, 13, 13, 13, 13, 13,  0,
                 5, 13, 13, 13,  0, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13
            ];

            var c = MemoryMarshal.GetReference(str);

            Debug.Assert(char.IsAscii(c), "Must already be validated");

            return (uint)str.Length + associatedValues[c];
        }

        static ReadOnlySpan<HttpMethod> Methods() =>
        [
            HttpMethod.None,
            HttpMethod.None,
            HttpMethod.None,
            HttpMethod.Get,
            HttpMethod.Head,
            HttpMethod.Trace,
            HttpMethod.Delete,
            HttpMethod.Options,
            HttpMethod.Put,
            HttpMethod.Post,
            HttpMethod.Patch,
            HttpMethod.None,
            HttpMethod.Connect
        ];
    }

    private static readonly string[] WordListForPerfectHashOfMethods =
    {
        "",
        "",
        "",
        "GET",
        "HEAD",
        "TRACE",
        "DELETE",
        "OPTIONS",
        "PUT",
        "POST",
        "PATCH",
        "",
        "CONNECT"
    };

    /// <summary>
    /// Checks 9 bytes from <paramref name="span"/>  correspond to a known HTTP version.
    /// </summary>
    /// <remarks>
    /// A "known HTTP version" Is is either HTTP/1.0 or HTTP/1.1.
    /// Since those fit in 8 bytes, they can be optimally looked up by reading those bytes as a long. Once
    /// in that format, it can be checked against the known versions.
    /// The Known versions will be checked with the required '\r'.
    /// To optimize performance the HTTP/1.1 will be checked first.
    /// </remarks>
    /// <returns><c>true</c> if the input matches a known string, <c>false</c> otherwise.</returns>
    public static bool GetKnownVersion(this ReadOnlySpan<byte> span, out HttpVersion knownVersion, out byte length)
    {
        if (span.Length > sizeof(ulong) && span[sizeof(ulong)] == (byte)'\r')
        {
            knownVersion = GetKnownVersion(span);
            if (knownVersion != HttpVersion.Unknown)
            {
                length = sizeof(ulong);
                return true;
            }
        }

        knownVersion = HttpVersion.Unknown;
        length = 0;
        return false;
    }

    /// <summary>
    /// Checks 8 bytes from <paramref name="span"/>  correspond to a known HTTP version.
    /// </summary>
    /// <remarks>
    /// A "known HTTP version" Is is either HTTP/1.0 or HTTP/1.1.
    /// Since those fit in 8 bytes, they can be optimally looked up by reading those bytes as a long. Once
    /// in that format, it can be checked against the known versions.
    /// To optimize performance the HTTP/1.1 will be checked first.
    /// </remarks>
    /// <returns>the HTTP version if the input matches a known string, <c>Unknown</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static HttpVersion GetKnownVersion(this ReadOnlySpan<byte> span)
    {
        if (BinaryPrimitives.TryReadUInt64LittleEndian(span, out var version))
        {
            if (version == _http11VersionLong)
            {
                return HttpVersion.Http11;
            }
            else if (version == _http10VersionLong)
            {
                return HttpVersion.Http10;
            }
        }
        return HttpVersion.Unknown;
    }

    /// <summary>
    /// Checks 8 bytes from <paramref name="span"/> that correspond to 'http://' or 'https://'
    /// </summary>
    /// <param name="span">The span</param>
    /// <param name="knownScheme">A reference to the known scheme, if the input matches any</param>
    /// <returns>True when memory starts with known http or https schema</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetKnownHttpScheme(this Span<byte> span, out HttpScheme knownScheme)
    {
        if (BinaryPrimitives.TryReadUInt64LittleEndian(span, out var scheme))
        {
            if ((scheme & _mask7Chars) == _httpSchemeLong)
            {
                knownScheme = HttpScheme.Http;
                return true;
            }

            if (scheme == _httpsSchemeLong)
            {
                knownScheme = HttpScheme.Https;
                return true;
            }
        }
        knownScheme = HttpScheme.Unknown;
        return false;
    }

    public static string VersionToString(HttpVersion httpVersion)
    {
        switch (httpVersion)
        {
            case HttpVersion.Http10:
                return AspNetCore.Http.HttpProtocol.Http10;
            case HttpVersion.Http11:
                return AspNetCore.Http.HttpProtocol.Http11;
            case HttpVersion.Http2:
                return AspNetCore.Http.HttpProtocol.Http2;
            case HttpVersion.Http3:
                return AspNetCore.Http.HttpProtocol.Http3;
            default:
                Debug.Fail("Unexpected HttpVersion: " + httpVersion);
                return null;
        };
    }

    public static string? MethodToString(HttpMethod method)
    {
        var methodIndex = (int)method;
        var methodNames = _methodNames;
        if ((uint)methodIndex < (uint)methodNames.Length)
        {
            return methodNames[methodIndex];
        }
        return null;
    }

    public static string? SchemeToString(HttpScheme scheme)
    {
        return scheme switch
        {
            HttpScheme.Http => HttpUriScheme,
            HttpScheme.Https => HttpsUriScheme,
            _ => null,
        };
    }

    public static bool IsHostHeaderValid(string hostText)
    {
        if (string.IsNullOrEmpty(hostText))
        {
            // The spec allows empty values
            return true;
        }

        var firstChar = hostText[0];
        if (firstChar == '[')
        {
            // Tail call
            return IsIPv6HostValid(hostText);
        }
        else
        {
            if (firstChar == ':')
            {
                // Only a port
                return false;
            }

            var invalid = HttpCharacters.IndexOfInvalidHostChar(hostText);
            if (invalid >= 0)
            {
                // Tail call
                return IsHostPortValid(hostText, invalid);
            }

            return true;
        }
    }

    // The lead '[' was already checked
    private static bool IsIPv6HostValid(string hostText)
    {
        for (var i = 1; i < hostText.Length; i++)
        {
            var ch = hostText[i];
            if (ch == ']')
            {
                // [::1] is the shortest valid IPv6 host
                if (i < 4)
                {
                    return false;
                }
                else if (i + 1 < hostText.Length)
                {
                    // Tail call
                    return IsHostPortValid(hostText, i + 1);
                }
                return true;
            }

            if (!IsHex(ch) && ch != ':' && ch != '.')
            {
                return false;
            }
        }

        // Must contain a ']'
        return false;
    }

    private static bool IsHostPortValid(string hostText, int offset)
    {
        var firstChar = hostText[offset];
        offset++;
        if (firstChar != ':' || offset == hostText.Length)
        {
            // Must have at least one number after the colon if present.
            return false;
        }

        for (var i = offset; i < hostText.Length; i++)
        {
            if (!IsNumeric(hostText[i]))
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNumeric(char ch)
    {
        // '0' <= ch && ch <= '9'
        // (uint)(ch - '0') <= (uint)('9' - '0')

        // Subtract start of range '0'
        // Cast to uint to change negative numbers to large numbers
        // Check if less than 10 representing chars '0' - '9'
        return (uint)(ch - '0') < 10u;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsHex(char ch)
    {
        return IsNumeric(ch)
            // || ('a' <= ch && ch <= 'f')
            // || ('A' <= ch && ch <= 'F');

            // Lowercase indiscriminately (or with 32)
            // Subtract start of range 'a'
            // Cast to uint to change negative numbers to large numbers
            // Check if less than 6 representing chars 'a' - 'f'
            || (uint)((ch | 32) - 'a') < 6u;
    }

    public static AltSvcHeader? GetEndpointAltSvc(System.Net.IPEndPoint endpoint, HttpProtocols protocols)
    {
        var hasHttp1OrHttp2 = protocols.HasFlag(HttpProtocols.Http1) || protocols.HasFlag(HttpProtocols.Http2);
        var hasHttp3 = protocols.HasFlag(HttpProtocols.Http3);

        if (hasHttp1OrHttp2 && hasHttp3)
        {
            // 86400 is a cache of 24 hours.
            // This is the default cache if none is specified with Alt-Svc, but it appears that all
            // popular HTTP/3 websites explicitly specifies a cache duration in the header.
            // Specify a value to be consistent.
            var text = "h3=\":" + endpoint.Port.ToString(CultureInfo.InvariantCulture) + "\"; ma=86400";
            var bytes = Encoding.ASCII.GetBytes($"\r\nAlt-Svc: " + text);
            return new AltSvcHeader(text, bytes);
        }

        return null;
    }
}

internal sealed class AltSvcHeader
{
    public string Value { get; }
    
    public byte[] RawBytes { get; }

    public AltSvcHeader(string value, byte[] rawBytes)
    {
        Value = value;
        RawBytes = rawBytes;
    }
}
