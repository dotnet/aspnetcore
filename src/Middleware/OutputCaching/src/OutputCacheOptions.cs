// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Options for configuring the <see cref="OutputCacheMiddleware"/>.
/// </summary>
public class OutputCacheOptions
{
    /// <summary>
    /// The size limit for the output cache middleware in bytes. The default is set to 100 MB.
    /// When this limit is exceeded, no new responses will be cached until older entries are
    /// evicted.
    /// </summary>
    public long SizeLimit { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// The largest cacheable size for the response body in bytes. The default is set to 64 MB.
    /// If the response body exceeds this limit, it will not be cached by the <see cref="OutputCacheMiddleware"/>.
    /// </summary>
    public long MaximumBodySize { get; set; } = 64 * 1024 * 1024;

    /// <summary>
    /// The duration a response is cached when no specific value is defined by a policy. The default is set to 60 seconds.
    /// </summary>
    public TimeSpan DefaultExpirationTimeSpan { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// <c>true</c> if request paths are case-sensitive; otherwise <c>false</c>. The default is to treat paths as case-insensitive.
    /// </summary>
    public bool UseCaseSensitivePaths { get; set; }

    /// <summary>
    /// Gets the application <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ApplicationServices { get; internal set; } = default!;

    internal Dictionary<string, IOutputCachePolicy>? NamedPolicies { get; set; }

    internal List<IOutputCachePolicy>? BasePolicies { get; set; }

    /// <summary>
    /// For testing purposes only.
    /// </summary>
    internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// Defines a <see cref="IOutputCachePolicy"/> which can be referenced by name.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="policy">The policy to add</param>
    public void AddPolicy(string name, IOutputCachePolicy policy)
    {
        NamedPolicies ??= new Dictionary<string, IOutputCachePolicy>(StringComparer.OrdinalIgnoreCase);
        NamedPolicies[name] = policy;
    }

    /// <summary>
    /// Defines a <see cref="IOutputCachePolicy"/> which can be referenced by name.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="build">An action on <see cref="OutputCachePolicyBuilder"/>.</param>
    /// <remarks>The built policy will be based on the default policy.</remarks>
    public void AddPolicy(string name, Action<OutputCachePolicyBuilder> build) => AddPolicy(name, build, false);

    /// <summary>
    /// Defines a <see cref="IOutputCachePolicy"/> which can be referenced by name.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="build">An action on <see cref="OutputCachePolicyBuilder"/>.</param>
    /// <param name="excludeDefaultPolicy">Whether to exclude the default policy or not.</param>
    public void AddPolicy(string name, Action<OutputCachePolicyBuilder> build, bool excludeDefaultPolicy)
    {
        var builder = new OutputCachePolicyBuilder(excludeDefaultPolicy);
        build(builder);
        NamedPolicies ??= new Dictionary<string, IOutputCachePolicy>(StringComparer.OrdinalIgnoreCase);
        NamedPolicies[name] = builder.Build();
    }

    /// <summary>
    /// Adds an <see cref="IOutputCachePolicy"/> instance to base policies.
    /// </summary>
    /// <param name="policy">The policy to add</param>
    public void AddBasePolicy(IOutputCachePolicy policy)
    {
        BasePolicies ??= new();
        BasePolicies.Add(policy);
    }

    /// <summary>
    /// Builds and adds an <see cref="IOutputCachePolicy"/> instance to base policies.
    /// </summary>
    /// <param name="build">An action on <see cref="OutputCachePolicyBuilder"/>.</param>
    /// <remarks>The built policy will be based on the default policy.</remarks>
    public void AddBasePolicy(Action<OutputCachePolicyBuilder> build) => AddBasePolicy(build, false);

    /// <summary>
    /// Builds and adds an <see cref="IOutputCachePolicy"/> instance to base policies.
    /// </summary>
    /// <param name="build">An action on <see cref="OutputCachePolicyBuilder"/>.</param>
    /// <param name="excludeDefaultPolicy">Whether to exclude the default policy or not.</param>
    public void AddBasePolicy(Action<OutputCachePolicyBuilder> build, bool excludeDefaultPolicy)
    {
        var builder = new OutputCachePolicyBuilder(excludeDefaultPolicy);
        build(builder);
        BasePolicies ??= new();
        BasePolicies.Add(builder.Build());
    }
}
