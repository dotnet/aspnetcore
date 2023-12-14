// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.AspNetCore.Http.Extensions;

/// <summary>
/// A helper class for constructing encoded Uris for use in headers and other Uris.
/// </summary>
public static class UriHelper
{
    private const char ForwardSlash = '/';
    private const char Hash = '#';
    private const char QuestionMark = '?';
    private static readonly string SchemeDelimiter = Uri.SchemeDelimiter;
    private static readonly SpanAction<char, (string scheme, string host, string pathBase, string path, string query, string fragment)> InitializeAbsoluteUriStringSpanAction = new(InitializeAbsoluteUriString);

    /// <summary>
    /// Combines the given URI components into a string that is properly encoded for use in HTTP headers.
    /// </summary>
    /// <param name="pathBase">The first portion of the request path associated with application root.</param>
    /// <param name="path">The portion of the request path that identifies the requested resource.</param>
    /// <param name="query">The query, if any.</param>
    /// <param name="fragment">The fragment, if any.</param>
    /// <returns>The combined URI components, properly encoded for use in HTTP headers.</returns>
    public static string BuildRelative(
        PathString pathBase = new PathString(),
        PathString path = new PathString(),
        QueryString query = new QueryString(),
        FragmentString fragment = new FragmentString())
    {
        string combinePath = (pathBase.HasValue || path.HasValue) ? (pathBase + path).ToString() : "/";
        return combinePath + query.ToString() + fragment.ToString();
    }

    /// <summary>
    /// Combines the given URI components into a string that is properly encoded for use in HTTP headers.
    /// Note that unicode in the HostString will be encoded as punycode.
    /// </summary>
    /// <param name="scheme">http, https, etc.</param>
    /// <param name="host">The host portion of the uri normally included in the Host header. This may include the port.</param>
    /// <param name="pathBase">The first portion of the request path associated with application root.</param>
    /// <param name="path">The portion of the request path that identifies the requested resource.</param>
    /// <param name="query">The query, if any.</param>
    /// <param name="fragment">The fragment, if any.</param>
    /// <returns>The combined URI components, properly encoded for use in HTTP headers.</returns>
    public static string BuildAbsolute(
        string scheme,
        HostString host,
        PathString pathBase = new PathString(),
        PathString path = new PathString(),
        QueryString query = new QueryString(),
        FragmentString fragment = new FragmentString())
    {
        ArgumentNullException.ThrowIfNull(scheme);

        var hostText = host.ToUriComponent();
        var pathBaseText = pathBase.ToUriComponent();
        var pathText = path.ToUriComponent();
        var queryText = query.ToUriComponent();
        var fragmentText = fragment.ToUriComponent();

        // PERF: Calculate string length to allocate correct buffer size for string.Create.
        var length =
            scheme.Length +
            Uri.SchemeDelimiter.Length +
            hostText.Length +
            pathBaseText.Length +
            pathText.Length +
            queryText.Length +
            fragmentText.Length;

        if (string.IsNullOrEmpty(pathText))
        {
            if (string.IsNullOrEmpty(pathBaseText))
            {
                pathText = "/";
                length++;
            }
        }
        else if (pathBaseText.EndsWith('/'))
        {
            // If the path string has a trailing slash and the other string has a leading slash, we need
            // to trim one of them.
            // Just decrement the total length, for now.
            length--;
        }

        return string.Create(length, (scheme, hostText, pathBaseText, pathText, queryText, fragmentText), InitializeAbsoluteUriStringSpanAction);
    }

    /// <summary>
    /// Separates the given absolute URI string into components. Assumes no PathBase.
    /// </summary>
    /// <param name="uri">A string representation of the uri.</param>
    /// <param name="scheme">http, https, etc.</param>
    /// <param name="host">The host portion of the uri normally included in the Host header. This may include the port.</param>
    /// <param name="path">The portion of the request path that identifies the requested resource.</param>
    /// <param name="query">The query, if any.</param>
    /// <param name="fragment">The fragment, if any.</param>
    public static void FromAbsolute(
        [StringSyntax(StringSyntaxAttribute.Uri)] string uri,
        out string scheme,
        out HostString host,
        out PathString path,
        out QueryString query,
        out FragmentString fragment)
    {
        ArgumentNullException.ThrowIfNull(uri);

        path = new PathString();
        query = new QueryString();
        fragment = new FragmentString();
        var startIndex = uri.IndexOf(SchemeDelimiter, StringComparison.Ordinal);

        if (startIndex < 0)
        {
            throw new FormatException("No scheme delimiter in uri.");
        }

        scheme = uri.Substring(0, startIndex);

        // PERF: Calculate the end of the scheme for next IndexOf
        startIndex += SchemeDelimiter.Length;

        int searchIndex;
        var limit = uri.Length;
        if ((searchIndex = uri.IndexOf(Hash, startIndex)) >= 0 && searchIndex < limit)
        {
            fragment = FragmentString.FromUriComponent(uri.Substring(searchIndex));
            limit = searchIndex;
        }

        if ((searchIndex = uri.IndexOf(QuestionMark, startIndex)) >= 0 && searchIndex < limit)
        {
            query = QueryString.FromUriComponent(uri.Substring(searchIndex, limit - searchIndex));
            limit = searchIndex;
        }

        if ((searchIndex = uri.IndexOf(ForwardSlash, startIndex)) >= 0 && searchIndex < limit)
        {
            path = PathString.FromUriComponent(uri.Substring(searchIndex, limit - searchIndex));
            limit = searchIndex;
        }

        host = HostString.FromUriComponent(uri.Substring(startIndex, limit - startIndex));
    }

    /// <summary>
    /// Generates a string from the given absolute or relative Uri that is appropriately encoded for use in
    /// HTTP headers. Note that a unicode host name will be encoded as punycode.
    /// </summary>
    /// <param name="uri">The Uri to encode.</param>
    /// <returns>The encoded string version of <paramref name="uri"/>.</returns>
    public static string Encode(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (uri.IsAbsoluteUri)
        {
            return BuildAbsolute(
                scheme: uri.Scheme,
                host: HostString.FromUriComponent(uri),
                pathBase: PathString.FromUriComponent(uri),
                query: QueryString.FromUriComponent(uri),
                fragment: FragmentString.FromUriComponent(uri));
        }
        else
        {
            return uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
        }
    }

    /// <summary>
    /// Returns the combined components of the request URL in a fully escaped form suitable for use in HTTP headers
    /// and other HTTP operations.
    /// </summary>
    /// <param name="request">The request to assemble the uri pieces from.</param>
    /// <returns>The encoded string version of the URL from <paramref name="request"/>.</returns>
    public static string GetEncodedUrl(this HttpRequest request)
    {
        return BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path, request.QueryString);
    }
    /// <summary>
    /// Returns the relative URI.
    /// </summary>
    /// <param name="request">The request to assemble the uri pieces from.</param>
    /// <returns>The path and query off of <paramref name="request"/>.</returns>
    public static string GetEncodedPathAndQuery(this HttpRequest request)
    {
        return BuildRelative(request.PathBase, request.Path, request.QueryString);
    }

    /// <summary>
    /// Returns the combined components of the request URL in a fully un-escaped form (except for the QueryString)
    /// suitable only for display. This format should not be used in HTTP headers or other HTTP operations.
    /// </summary>
    /// <param name="request">The request to assemble the uri pieces from.</param>
    /// <returns>The combined components of the request URL in a fully un-escaped form (except for the QueryString)
    /// suitable only for display.</returns>
    public static string GetDisplayUrl(this HttpRequest request)
    {
        var scheme = request.Scheme ?? string.Empty;
        var host = request.Host.Value ?? string.Empty;
        var pathBase = request.PathBase.Value ?? string.Empty;
        var path = request.Path.Value ?? string.Empty;
        var queryString = request.QueryString.Value ?? string.Empty;

        // PERF: Calculate string length to allocate correct buffer size for StringBuilder.
        var length = scheme.Length + SchemeDelimiter.Length + host.Length
            + pathBase.Length + path.Length + queryString.Length;

        return new StringBuilder(length)
            .Append(scheme)
            .Append(SchemeDelimiter)
            .Append(host)
            .Append(pathBase)
            .Append(path)
            .Append(queryString)
            .ToString();
    }

    /// <summary>
    /// Copies the specified <paramref name="text"/> to the specified <paramref name="buffer"/> starting at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="buffer">The buffer to copy text to.</param>
    /// <param name="index">The buffer start index.</param>
    /// <param name="text">The text to copy.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyTextToBuffer(Span<char> buffer, int index, ReadOnlySpan<char> text)
    {
        text.CopyTo(buffer.Slice(index, text.Length));
        return index + text.Length;
    }

    /// <summary>
    /// Initializes the URI <see cref="string"/> for <see cref="BuildAbsolute(string, HostString, PathString, PathString, QueryString, FragmentString)"/>.
    /// </summary>
    /// <param name="buffer">The URI <see cref="string"/>'s <see cref="char"/> buffer.</param>
    /// <param name="uriParts">The URI parts.</param>
    private static void InitializeAbsoluteUriString(Span<char> buffer, (string scheme, string host, string pathBase, string path, string query, string fragment) uriParts)
    {
        var index = 0;

        var pathBaseSpan = uriParts.pathBase.AsSpan();

        if (uriParts.path.Length > 0 && pathBaseSpan.Length > 0 && pathBaseSpan[^1] == '/')
        {
            // If the path string has a trailing slash and the other string has a leading slash, we need
            // to trim one of them.
            // Trim the last slahs from pathBase. The total length was decremented before the call to string.Create.
            pathBaseSpan = pathBaseSpan[..^1];
        }

        index = CopyTextToBuffer(buffer, index, uriParts.scheme.AsSpan());
        index = CopyTextToBuffer(buffer, index, Uri.SchemeDelimiter.AsSpan());
        index = CopyTextToBuffer(buffer, index, uriParts.host.AsSpan());
        index = CopyTextToBuffer(buffer, index, pathBaseSpan);
        index = CopyTextToBuffer(buffer, index, uriParts.path.AsSpan());
        index = CopyTextToBuffer(buffer, index, uriParts.query.AsSpan());
        _ = CopyTextToBuffer(buffer, index, uriParts.fragment.AsSpan());
    }
}
