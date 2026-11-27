// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Stores responses the way the Response Caching middleware does, so the response <c>Vary</c> header and the query keys set in
/// <see cref="IResponseCachingFeature"/> extend the cache key.
/// </summary>
/// <remarks>
/// Two entries are stored for each response:
/// <list type="bullet">
/// <item>The vary rules, under <see cref="OutputCacheContext.CacheKey"/> and a suffix: a random prefix, the header names listed by the response <c>Vary</c> header, and the query keys.</item>
/// <item>The response, under <see cref="OutputCacheContext.CacheKey"/>, the prefix, and the request values of these headers and query keys.</item>
/// </list>
/// The vary rules are written again with each response, so they follow the latest response. When they change, a new prefix is used,
/// so the responses stored with the previous rules can't be found anymore.
/// </remarks>
internal sealed class ResponseCachingOutputCacheEntryStrategy : IOutputCacheEntryStrategy
{
    private const char KeyDelimiter = OutputCacheKeyProvider.KeyDelimiter;
    private const char KeySubDelimiter = OutputCacheKeyProvider.KeySubDelimiter;
    private const char KeyNameValueDelimiter = OutputCacheKeyProvider.KeyNameValueDelimiter;

    // Default cache keys only contain single letter segments after the delimiter, so this suffix can't match one.
    private const string VaryByRulesKeySuffix = "VaryByRules";

    private readonly IOutputCacheStore _store;
    private readonly ILogger _logger;

    public ResponseCachingOutputCacheEntryStrategy(IOutputCacheStore store, ILogger logger)
    {
        _store = store;
        _logger = logger;
    }

    public async ValueTask<OutputCacheEntry?> GetAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var varyByRules = await GetVaryByRulesAsync(context.CacheKey, cancellationToken);
        if (varyByRules is null || !TryCreateResponseKey(context, varyByRules, out var responseKey))
        {
            return null;
        }

        return await OutputCacheEntryFormatter.GetAsync(responseKey, _store, cancellationToken);
    }

    public async ValueTask StoreAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        // Like Response Caching, read the Vary header captured when the response started rather than the one sent to the client:
        // headers added later, e.g. by Response Compression in an OnStarting callback, aren't part of the vary rules.
        var varyHeaderNames = GetNormalizedNames(SplitCommaSeparatedValues(context.CachedResponse!.FindHeader(HeaderNames.Vary)));
        var responseCachingFeature = context.HttpContext.Features.Get<IResponseCachingFeature>();
        var varyQueryKeys = GetNormalizedNames(responseCachingFeature is ResponseCachingFeature feature ? feature.VaryByQueryKeysWhenResponseStarted : responseCachingFeature?.VaryByQueryKeys);
        if (ContainsDelimiters(varyHeaderNames) || ContainsDelimiters(varyQueryKeys))
        {
            return;
        }

        VaryByRules? varyByRules;
        try
        {
            varyByRules = await GetVaryByRulesAsync(context.CacheKey, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.UnableToQueryOutputCache(ex);
            varyByRules = null;
        }

        // Keep the prefix while the rules are the same, so the responses stored for other header and query values remain valid.
        if (varyByRules is null
            || !varyByRules.HeaderNames.AsSpan().SequenceEqual(varyHeaderNames)
            || !varyByRules.QueryKeys.AsSpan().SequenceEqual(varyQueryKeys))
        {
            varyByRules = new VaryByRules(Guid.NewGuid().ToString("N"), varyHeaderNames, varyQueryKeys);
        }

        if (!TryCreateResponseKey(context, varyByRules, out var responseKey))
        {
            return;
        }

        try
        {
            // Always written, to extend the expiration of the rules to the one of the latest response.
            await _store.SetAsync(GetVaryByRulesKey(context.CacheKey), varyByRules.Serialize(), tags: null, context.CachedResponseValidFor, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.UnableToWriteToOutputCache(ex);
            return;
        }

        await OutputCacheEntryFormatter.StoreAsync(responseKey, context.CachedResponse!, context.Tags, context.CachedResponseValidFor, _store, _logger, cancellationToken);
    }

    private async ValueTask<VaryByRules?> GetVaryByRulesAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var content = await _store.GetAsync(GetVaryByRulesKey(cacheKey), cancellationToken);
        return content is null ? null : VaryByRules.Deserialize(content);
    }

    private static string GetVaryByRulesKey(string cacheKey) => string.Concat(cacheKey, KeyDelimiter.ToString(), VaryByRulesKeySuffix);

    // <CacheKey><delimiter><Prefix>
    // <delimiter>H<delimiter>HeaderName<key-value-delimiter>HeaderValue1<subdelimiter>HeaderValue2
    // <delimiter>Q<delimiter>QueryName<key-value-delimiter>QueryValue1<subdelimiter>QueryValue2
    private static bool TryCreateResponseKey(OutputCacheContext context, VaryByRules varyByRules, out string responseKey)
    {
        responseKey = string.Empty;

        var builder = new StringBuilder(context.CacheKey)
            .Append(KeyDelimiter)
            .Append(varyByRules.Prefix);

        var request = context.HttpContext.Request;

        if (varyByRules.HeaderNames.Length > 0)
        {
            builder.Append(KeyDelimiter).Append('H');

            foreach (var headerName in varyByRules.HeaderNames)
            {
                if (!TryAppendNameAndValues(builder, headerName, request.Headers[headerName]))
                {
                    return false;
                }
            }
        }

        if (varyByRules.QueryKeys.Length > 0)
        {
            builder.Append(KeyDelimiter).Append('Q');

            if (varyByRules.QueryKeys is ["*"])
            {
                // Vary by all available query keys. Query keys are aggregated case-insensitively whereas the query values are compared ordinally.
                var query = request.Query.ToArray();
                Array.Sort(query, static (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Key, y.Key));

                foreach (var (queryKey, queryValues) in query)
                {
                    if (OutputCacheKeyProvider.ContainsDelimiters(queryKey) || !TryAppendNameAndValues(builder, queryKey.ToUpperInvariant(), queryValues))
                    {
                        return false;
                    }
                }
            }
            else
            {
                foreach (var queryKey in varyByRules.QueryKeys)
                {
                    if (!TryAppendNameAndValues(builder, queryKey, request.Query[queryKey]))
                    {
                        return false;
                    }
                }
            }
        }

        responseKey = builder.ToString();
        return true;
    }

    private static bool TryAppendNameAndValues(StringBuilder builder, string name, StringValues values)
    {
        builder.Append(KeyDelimiter).Append(name).Append(KeyNameValueDelimiter);

        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (OutputCacheKeyProvider.ContainsDelimiters(value))
            {
                return false;
            }

            if (i > 0)
            {
                builder.Append(KeySubDelimiter);
            }

            builder.Append(value);
        }

        return true;
    }

    // Same normalization as Response Caching: upper case and ordinal order, so the order and casing of the names don't create new rules.
    private static string[] SplitCommaSeparatedValues(StringValues values)
    {
        var result = new List<string>();
        foreach (var value in values)
        {
            if (!string.IsNullOrEmpty(value))
            {
                result.AddRange(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }

        return result.ToArray();
    }

    private static string[] GetNormalizedNames(string[]? names)
    {
        if (names is null || names.Length == 0)
        {
            return [];
        }

        var normalizedNames = new List<string>(names.Length);
        foreach (var name in names)
        {
            if (!string.IsNullOrEmpty(name))
            {
                normalizedNames.Add(name.ToUpperInvariant());
            }
        }

        normalizedNames.Sort(StringComparer.Ordinal);
        return normalizedNames.ToArray();
    }

    private static bool ContainsDelimiters(string[] names)
    {
        foreach (var name in names)
        {
            if (OutputCacheKeyProvider.ContainsDelimiters(name))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class VaryByRules(string prefix, string[] headerNames, string[] queryKeys)
    {
        public string Prefix { get; } = prefix;

        public string[] HeaderNames { get; } = headerNames;

        public string[] QueryKeys { get; } = queryKeys;

        // Format: Prefix<delimiter>HeaderName1<subdelimiter>HeaderName2<delimiter>QueryKey1<subdelimiter>QueryKey2
        public byte[] Serialize()
        {
            var value = string.Concat(Prefix, KeyDelimiter.ToString(), string.Join(KeySubDelimiter, HeaderNames), KeyDelimiter.ToString(), string.Join(KeySubDelimiter, QueryKeys));
            return Encoding.UTF8.GetBytes(value);
        }

        public static VaryByRules? Deserialize(byte[] content)
        {
            var segments = Encoding.UTF8.GetString(content).Split(KeyDelimiter);
            if (segments.Length != 3)
            {
                return null;
            }

            return new VaryByRules(
                segments[0],
                segments[1].Split(KeySubDelimiter, StringSplitOptions.RemoveEmptyEntries),
                segments[2].Split(KeySubDelimiter, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}