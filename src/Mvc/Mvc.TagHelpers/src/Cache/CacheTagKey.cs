// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.TagHelpers.Cache;

/// <summary>
/// An instance of <see cref="CacheTagKey"/> represents the state of <see cref="CacheTagHelper"/>
/// or <see cref="DistributedCacheTagHelper"/> keys.
/// </summary>
public class CacheTagKey : IEquatable<CacheTagKey>
{
    private static readonly char[] AttributeSeparator = new[] { ',' };
    private static readonly Func<IRequestCookieCollection, string, string> CookieAccessor = (c, key) => c[key];
    private static readonly Func<IHeaderDictionary, string, string> HeaderAccessor = (c, key) => c[key];
    private static readonly Func<IQueryCollection, string, string> QueryAccessor = (c, key) => c[key];
    private static readonly Func<RouteValueDictionary, string, string> RouteValueAccessor = (c, key) =>
        Convert.ToString(c[key], CultureInfo.InvariantCulture);

    private const string CacheKeyTokenSeparator = "||";
    private const string VaryByName = "VaryBy";
    private const string VaryByHeaderName = "VaryByHeader";
    private const string VaryByQueryName = "VaryByQuery";
    private const string VaryByRouteName = "VaryByRoute";
    private const string VaryByCookieName = "VaryByCookie";
    private const string VaryByUserName = "VaryByUser";
    private const string VaryByCulture = "VaryByCulture";

    private readonly string _prefix;
    private readonly string _varyBy;
    private readonly DateTimeOffset? _expiresOn;
    private readonly TimeSpan? _expiresAfter;
    private readonly TimeSpan? _expiresSliding;
    private readonly IList<KeyValuePair<string, string>> _headers;
    private readonly IList<KeyValuePair<string, string>> _queries;
    private readonly IList<KeyValuePair<string, string>> _routeValues;
    private readonly IList<KeyValuePair<string, string>> _cookies;
    private readonly bool _varyByUser;
    private readonly bool _varyByCulture;
    private readonly string _username;
    private readonly CultureInfo _requestCulture;
    private readonly CultureInfo _requestUICulture;

    private string _generatedKey;
    private int? _hashcode;

    /// <summary>
    /// Creates an instance of <see cref="CacheTagKey"/> for a specific <see cref="CacheTagHelper"/>.
    /// </summary>
    /// <param name="tagHelper">The <see cref="CacheTagHelper"/>.</param>
    /// <param name="context">The <see cref="TagHelperContext"/>.</param>
    /// <returns>A new <see cref="CacheTagKey"/>.</returns>
    public CacheTagKey(CacheTagHelper tagHelper, TagHelperContext context)
        : this(tagHelper)
    {
        Key = context.UniqueId;
        _prefix = nameof(CacheTagHelper);
    }

    /// <summary>
    /// Creates an instance of <see cref="CacheTagKey"/> for a specific <see cref="DistributedCacheTagHelper"/>.
    /// </summary>
    /// <param name="tagHelper">The <see cref="DistributedCacheTagHelper"/>.</param>
    /// <returns>A new <see cref="CacheTagKey"/>.</returns>
    public CacheTagKey(DistributedCacheTagHelper tagHelper)
        : this((CacheTagHelperBase)tagHelper)
    {
        Key = tagHelper.Name;
        _prefix = nameof(DistributedCacheTagHelper);
    }

    private CacheTagKey(CacheTagHelperBase tagHelper)
    {
        var httpContext = tagHelper.ViewContext.HttpContext;
        var request = httpContext.Request;

        _expiresAfter = tagHelper.ExpiresAfter;
        _expiresOn = tagHelper.ExpiresOn;
        _expiresSliding = tagHelper.ExpiresSliding;
        _varyBy = tagHelper.VaryBy;
        _cookies = ExtractCollection(tagHelper.VaryByCookie, request.Cookies, CookieAccessor);
        _headers = ExtractCollection(tagHelper.VaryByHeader, request.Headers, HeaderAccessor);
        _queries = ExtractCollection(tagHelper.VaryByQuery, request.Query, QueryAccessor);
        _routeValues = ExtractCollection(
            tagHelper.VaryByRoute,
            tagHelper.ViewContext.RouteData.Values,
            RouteValueAccessor);
        _varyByUser = tagHelper.VaryByUser;
        _varyByCulture = tagHelper.VaryByCulture;

        if (_varyByUser)
        {
            _username = httpContext.User?.Identity?.Name;
        }

        if (_varyByCulture)
        {
            _requestCulture = CultureInfo.CurrentCulture;
            _requestUICulture = CultureInfo.CurrentUICulture;
        }
    }

    // Internal for unit testing.
    internal string Key { get; }

    /// <summary>
    /// Creates a <see cref="string"/> representation of the key.
    /// </summary>
    /// <returns>A <see cref="string"/> uniquely representing the key.</returns>
    public string GenerateKey()
    {
        // Caching as the key is immutable and it can be called multiple times during a request.
        if (_generatedKey != null)
        {
            return _generatedKey;
        }

        var builder = new StringBuilder(_prefix);
        builder
            .Append(CacheKeyTokenSeparator)
            .Append(Key);

        if (!string.IsNullOrEmpty(_varyBy))
        {
            builder
                .Append(CacheKeyTokenSeparator)
                .Append(VaryByName)
                .Append(CacheKeyTokenSeparator)
                .Append(_varyBy);
        }

        AddStringCollection(builder, VaryByCookieName, _cookies);
        AddStringCollection(builder, VaryByHeaderName, _headers);
        AddStringCollection(builder, VaryByQueryName, _queries);
        AddStringCollection(builder, VaryByRouteName, _routeValues);

        if (_varyByUser)
        {
            builder
                .Append(CacheKeyTokenSeparator)
                .Append(VaryByUserName)
                .Append(CacheKeyTokenSeparator)
                .Append(_username);
        }

        if (_varyByCulture)
        {
            builder
                .Append(CacheKeyTokenSeparator)
                .Append(VaryByCulture)
                .Append(CacheKeyTokenSeparator)
                .Append(_requestCulture)
                .Append(CacheKeyTokenSeparator)
                .Append(_requestUICulture);
        }

        _generatedKey = builder.ToString();

        return _generatedKey;
    }

    /// <summary>
    /// Creates a hashed value of the key.
    /// </summary>
    /// <returns>A cryptographic hash of the key.</returns>
    public string GenerateHashedKey()
    {
        var key = GenerateKey();

        // The key is typically too long to be useful, so we use a cryptographic hash
        // as the actual key (better randomization and key distribution, so small vary
        // values will generate dramatically different keys).
        var contentBytes = Encoding.UTF8.GetBytes(key);
        var hashedBytes = SHA256.HashData(contentBytes);
        return Convert.ToBase64String(hashedBytes);
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (obj is CacheTagKey other)
        {
            return Equals(other);
        }

        return false;
    }

    /// <inheritdoc />
    public bool Equals(CacheTagKey other)
    {
        return string.Equals(other.Key, Key, StringComparison.Ordinal) &&
            other._expiresAfter == _expiresAfter &&
            other._expiresOn == _expiresOn &&
            other._expiresSliding == _expiresSliding &&
            string.Equals(other._varyBy, _varyBy, StringComparison.Ordinal) &&
            AreSame(_cookies, other._cookies) &&
            AreSame(_headers, other._headers) &&
            AreSame(_queries, other._queries) &&
            AreSame(_routeValues, other._routeValues) &&
            (_varyByUser == other._varyByUser &&
                (!_varyByUser || string.Equals(other._username, _username, StringComparison.Ordinal))) &&
            CultureEquals();

        bool CultureEquals()
        {
            if (_varyByCulture != other._varyByCulture)
            {
                return false;
            }

            if (!_varyByCulture)
            {
                // Neither has culture set.
                return true;
            }

            return _requestCulture.Equals(other._requestCulture) &&
                _requestUICulture.Equals(other._requestUICulture);
        }
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // The hashcode is intentionally not using the computed
        // stringified key in order to prevent string allocations
        // in the common case where it's not explicitly required.

        // Caching as the key is immutable and it can be called
        // multiple times during a request.
        if (_hashcode.HasValue)
        {
            return _hashcode.Value;
        }

        var hashCode = new HashCode();

        hashCode.Add(Key, StringComparer.Ordinal);
        hashCode.Add(_expiresAfter);
        hashCode.Add(_expiresOn);
        hashCode.Add(_expiresSliding);
        hashCode.Add(_varyBy, StringComparer.Ordinal);
        hashCode.Add(_username, StringComparer.Ordinal);
        hashCode.Add(_requestCulture);
        hashCode.Add(_requestUICulture);

        CombineCollectionHashCode(ref hashCode, VaryByCookieName, _cookies);
        CombineCollectionHashCode(ref hashCode, VaryByHeaderName, _headers);
        CombineCollectionHashCode(ref hashCode, VaryByQueryName, _queries);
        CombineCollectionHashCode(ref hashCode, VaryByRouteName, _routeValues);

        _hashcode = hashCode.ToHashCode();

        return _hashcode.Value;
    }

    private static IList<KeyValuePair<string, string>> ExtractCollection<TSourceCollection>(
        string keys,
        TSourceCollection collection,
        Func<TSourceCollection, string, string> accessor)
    {
        if (string.IsNullOrEmpty(keys))
        {
            return null;
        }

        var tokenizer = new StringTokenizer(keys, AttributeSeparator);

        var result = new List<KeyValuePair<string, string>>();

        foreach (var item in tokenizer)
        {
            var trimmedValue = item.Trim();

            if (trimmedValue.Length != 0)
            {
                var value = accessor(collection, trimmedValue.Value);
                result.Add(new KeyValuePair<string, string>(trimmedValue.Value, value ?? string.Empty));
            }
        }

        return result;
    }

    private static void AddStringCollection(
        StringBuilder builder,
        string collectionName,
        IList<KeyValuePair<string, string>> values)
    {
        if (values == null || values.Count == 0)
        {
            return;
        }

        // keyName(param1=value1|param2=value2)
        builder
            .Append(CacheKeyTokenSeparator)
            .Append(collectionName)
            .Append('(');

        for (var i = 0; i < values.Count; i++)
        {
            var item = values[i];

            if (i > 0)
            {
                builder.Append(CacheKeyTokenSeparator);
            }

            builder
                .Append(item.Key)
                .Append(CacheKeyTokenSeparator)
                .Append(item.Value);
        }

        builder.Append(')');
    }

    private static void CombineCollectionHashCode(
        ref HashCode hashCode,
        string collectionName,
        IList<KeyValuePair<string, string>> values)
    {
        if (values != null)
        {
            hashCode.Add(collectionName, StringComparer.Ordinal);

            for (var i = 0; i < values.Count; i++)
            {
                var item = values[i];
                hashCode.Add(item.Key);
                hashCode.Add(item.Value);
            }
        }
    }

    private static bool AreSame(IList<KeyValuePair<string, string>> values1, IList<KeyValuePair<string, string>> values2)
    {
        if (values1 == values2)
        {
            return true;
        }

        if (values1 == null || values2 == null || values1.Count != values2.Count)
        {
            return false;
        }

        for (var i = 0; i < values1.Count; i++)
        {
            if (!string.Equals(values1[i].Key, values2[i].Key, StringComparison.Ordinal) ||
                !string.Equals(values1[i].Value, values2[i].Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
