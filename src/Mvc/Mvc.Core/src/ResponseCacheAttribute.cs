// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies the parameters necessary for setting appropriate headers in response caching.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ResponseCacheAttribute : Attribute, IFilterFactory, IOrderedFilter
{
    // A nullable-int cannot be used as an Attribute parameter.
    // Hence this nullable-int is present to back the Duration property.
    // The same goes for nullable-ResponseCacheLocation and nullable-bool.
    private int? _duration;
    private ResponseCacheLocation? _location;
    private bool? _noStore;

    /// <summary>
    /// Gets or sets the duration in seconds for which the response is cached.
    /// This sets "max-age" in "Cache-control" header.
    /// </summary>
    public int Duration
    {
        get => _duration ?? 0;
        set => _duration = value;
    }

    /// <summary>
    /// Gets or sets the location where the data from a particular URL must be cached.
    /// </summary>
    public ResponseCacheLocation Location
    {
        get => _location ?? ResponseCacheLocation.Any;
        set => _location = value;
    }

    /// <summary>
    /// Gets or sets the value which determines whether the data should be stored or not.
    /// When set to <see langword="true"/>, it sets "Cache-control" header to "no-store".
    /// Ignores the "Location" parameter for values other than "None".
    /// Ignores the "duration" parameter.
    /// </summary>
    public bool NoStore
    {
        get => _noStore ?? false;
        set => _noStore = value;
    }

    /// <summary>
    /// Gets or sets the value for the Vary response header.
    /// </summary>
    public string? VaryByHeader { get; set; }

    /// <summary>
    /// Gets or sets the query keys to vary by.
    /// </summary>
    /// <remarks>
    /// <see cref="VaryByQueryKeys"/> requires the response cache middleware.
    /// </remarks>
    public string[]? VaryByQueryKeys { get; set; }

    /// <summary>
    /// Gets or sets the value of the cache profile name.
    /// </summary>
    public string? CacheProfileName { get; set; }

    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public bool IsReusable => true;

    /// <summary>
    /// Gets the <see cref="CacheProfile"/> for this attribute.
    /// </summary>
    /// <returns></returns>
    public CacheProfile GetCacheProfile(MvcOptions options)
    {
        CacheProfile? selectedProfile = null;
        if (CacheProfileName != null)
        {
            options.CacheProfiles.TryGetValue(CacheProfileName, out selectedProfile);
            if (selectedProfile == null)
            {
                throw new InvalidOperationException(Resources.FormatCacheProfileNotFound(CacheProfileName));
            }
        }

        // If the ResponseCacheAttribute parameters are set,
        // then it must override the values from the Cache Profile.
        // The below expression first checks if the duration is set by the attribute's parameter.
        // If absent, it checks the selected cache profile (Note: There can be no cache profile as well)
        // The same is the case for other properties.
        _duration = _duration ?? selectedProfile?.Duration;
        _noStore = _noStore ?? selectedProfile?.NoStore;
        _location = _location ?? selectedProfile?.Location;
        VaryByHeader = VaryByHeader ?? selectedProfile?.VaryByHeader;
        VaryByQueryKeys = VaryByQueryKeys ?? selectedProfile?.VaryByQueryKeys;

        return new CacheProfile
        {
            Duration = _duration,
            Location = _location,
            NoStore = _noStore,
            VaryByHeader = VaryByHeader,
            VaryByQueryKeys = VaryByQueryKeys,
        };
    }

    /// <inheritdoc />
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var optionsAccessor = serviceProvider.GetRequiredService<IOptions<MvcOptions>>();
        var cacheProfile = GetCacheProfile(optionsAccessor.Value);

        // ResponseCacheFilter cannot take any null values. Hence, if there are any null values,
        // the properties convert them to their defaults and are passed on.
        return new ResponseCacheFilter(cacheProfile, loggerFactory);
    }
}
