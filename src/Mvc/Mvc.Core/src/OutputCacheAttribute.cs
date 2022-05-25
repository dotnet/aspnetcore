// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OutputCaching;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies the parameters necessary for setting appropriate headers in output caching.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class OutputCacheAttribute : Attribute, IOrderedFilter, IPoliciesMetadata
{
    // A nullable-int cannot be used as an Attribute parameter.
    // Hence this nullable-int is present to back the Duration property.
    // The same goes for nullable-ResponseCacheLocation and nullable-bool.
    private int? _duration;
    private bool? _noStore;

    private IOutputCachingPolicy? _policy;

    /// <summary>
    /// Gets or sets the duration in seconds for which the response is cached.
    /// </summary>
    public int Duration
    {
        get => _duration ?? 0;
        set => _duration = value;
    }

    /// <summary>
    /// Gets or sets the value which determines whether the data should be stored or not.
    /// When set to <see langword="true"/>, the response won't be cached.
    /// </summary>
    public bool NoStore
    {
        get => _noStore ?? false;
        set => _noStore = value;
    }

    /// <summary>
    /// Gets or sets the query keys to vary by.
    /// </summary>
    /// <remarks>
    /// <see cref="VaryByQueryKeys"/> requires the output cache middleware.
    /// </remarks>
    public string[]? VaryByQueryKeys { get; set; }

    /// <summary>
    /// Gets or sets the value of the cache profile name.
    /// </summary>
    public string? Profile { get; set; }

    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public IOutputCachingPolicy Policy => _policy ??= GetPolicy();

    private IOutputCachingPolicy GetPolicy()
    {
        var builder = new OutputCachePolicyBuilder().Enable();

        if (_noStore != null && _noStore.Value)
        {
            builder.NoStore();
        }

        if (Profile != null)
        {
            builder.Profile(Profile);
        }

        if (VaryByQueryKeys != null)
        {
            builder.VaryByQuery(VaryByQueryKeys);
        }

        if (_duration != null)
        {
            builder.Expire(TimeSpan.FromSeconds(_duration.Value));
        }

        return builder.Build();
    }
}
