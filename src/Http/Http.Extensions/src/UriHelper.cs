// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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

        return string.Create(
            length,
            new UriComponents(scheme, hostText, pathBaseText, pathText, queryText, fragmentText),
            static (buffer, uriComponents) =>
            {
                uriComponents.Scheme.AsSpan().CopyTo(buffer);
                buffer = buffer.Slice(uriComponents.Scheme.Length);

                Uri.SchemeDelimiter.CopyTo(buffer);
                buffer = buffer.Slice(Uri.SchemeDelimiter.Length);

                uriComponents.Host.AsSpan().CopyTo(buffer);
                buffer = buffer.Slice(uriComponents.Host.Length);

                var pathBaseSpan = uriComponents.PathBase.AsSpan();

                if (uriComponents.Path.Length > 0 && pathBaseSpan.Length > 0 && pathBaseSpan[^1] == '/')
                {
                    // If the path string has a trailing slash and the other string has a leading slash, we need
                    // to trim one of them.
                    // Trim the last slash from pathBase. The total length was decremented before the call to string.Create.
                    pathBaseSpan = pathBaseSpan[..^1];
                }

                pathBaseSpan.CopyTo(buffer);
                buffer = buffer.Slice(pathBaseSpan.Length);

                uriComponents.Path.CopyTo(buffer);
                buffer = buffer.Slice(uriComponents.Path.Length);

                uriComponents.Query.CopyTo(buffer);
                buffer = buffer.Slice(uriComponents.Query.Length);

                uriComponents.Fragment.CopyTo(buffer);
            });
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

    private readonly struct UriComponents
    {
        public readonly string Scheme;
        public readonly string Host;
        public readonly string PathBase;
        public readonly string Path;
        public readonly string Query;
        public readonly string Fragment;

        public UriComponents(string scheme, string host, string pathBase, string path, string query, string fragment)
        {
            Scheme = scheme;
            Host = host;
            PathBase = pathBase;
            Path = path;
            Query = query;
            Fragment = fragment;
        }
    }
}
