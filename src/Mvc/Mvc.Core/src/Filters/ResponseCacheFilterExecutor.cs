// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Filters;

internal sealed class ResponseCacheFilterExecutor
{
    private readonly CacheProfile _cacheProfile;
    private int? _cacheDuration;
    private ResponseCacheLocation? _cacheLocation;
    private bool? _cacheNoStore;
    private string? _cacheVaryByHeader;
    private string[]? _cacheVaryByQueryKeys;

    public ResponseCacheFilterExecutor(CacheProfile cacheProfile)
    {
        _cacheProfile = cacheProfile ?? throw new ArgumentNullException(nameof(cacheProfile));
    }

    public int Duration
    {
        get => _cacheDuration ?? _cacheProfile.Duration ?? 0;
        set => _cacheDuration = value;
    }

    public ResponseCacheLocation Location
    {
        get => _cacheLocation ?? _cacheProfile.Location ?? ResponseCacheLocation.Any;
        set => _cacheLocation = value;
    }

    public bool NoStore
    {
        get => _cacheNoStore ?? _cacheProfile.NoStore ?? false;
        set => _cacheNoStore = value;
    }

    public string? VaryByHeader
    {
        get => _cacheVaryByHeader ?? _cacheProfile.VaryByHeader;
        set => _cacheVaryByHeader = value;
    }

    public string[]? VaryByQueryKeys
    {
        get => _cacheVaryByQueryKeys ?? _cacheProfile.VaryByQueryKeys;
        set => _cacheVaryByQueryKeys = value;
    }

    public void Execute(FilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!(NoStore || _cacheProfile.Location == ResponseCacheLocation.None || _cacheLocation == ResponseCacheLocation.None))
        {
            // Duration MUST be set (either in the cache profile or in this filter) unless NoStore is true or Location is ResponseCacheLocation.None.
            if (_cacheProfile.Duration == null && _cacheDuration == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatResponseCache_SpecifyDuration(nameof(NoStore), nameof(Duration)));
            }
        }

        var headers = context.HttpContext.Response.Headers;

        // Clear all headers
        headers.Remove(HeaderNames.Vary);
        headers.Remove(HeaderNames.CacheControl);
        headers.Remove(HeaderNames.Pragma);

        if (!string.IsNullOrEmpty(VaryByHeader))
        {
            headers.Vary = VaryByHeader;
        }

        if (VaryByQueryKeys != null)
        {
            var responseCachingFeature = context.HttpContext.Features.Get<IResponseCachingFeature>();
            if (responseCachingFeature == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatVaryByQueryKeys_Requires_ResponseCachingMiddleware(nameof(VaryByQueryKeys)));
            }
            responseCachingFeature.VaryByQueryKeys = VaryByQueryKeys;
        }

        if (NoStore)
        {
            headers.CacheControl = "no-store";

            // Cache-control: no-store, no-cache is valid.
            if (Location == ResponseCacheLocation.None)
            {
                headers.AppendCommaSeparatedValues(HeaderNames.CacheControl, "no-cache");
                headers.Pragma = "no-cache";
            }
        }
        else
        {
            string? cacheControlValue;

            if (Location == ResponseCacheLocation.None && _cacheProfile.Duration == null && _cacheDuration == null)
            {
                cacheControlValue = "no-cache";
                headers.Pragma = "no-cache";
            }
            else
            {
                cacheControlValue = Location switch
                {
                    ResponseCacheLocation.Any => "public",
                    ResponseCacheLocation.Client => "private",
                    ResponseCacheLocation.None => "no-cache",
                    _ => null
                };
                cacheControlValue = $"{cacheControlValue},max-age={Duration}";
            }

            headers.CacheControl = cacheControlValue;
        }
    }
}
