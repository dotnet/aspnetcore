// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.OutputCaching.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Options for configuring the <see cref="OutputCachingMiddleware"/>.
/// </summary>
public class OutputCachingOptions
{
    /// <summary>
    /// Creates a new <see cref="OutputCachingOptions"/> instance.
    /// </summary>
    public OutputCachingOptions()
    {
        BasePolicies = new(this);
    }

    /// <summary>
    /// The size limit for the output cache middleware in bytes. The default is set to 100 MB.
    /// When this limit is exceeded, no new responses will be cached until older entries are
    /// evicted.
    /// </summary>
    public long SizeLimit { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// The largest cacheable size for the response body in bytes. The default is set to 64 MB.
    /// If the response body exceeds this limit, it will not be cached by the <see cref="OutputCachingMiddleware"/>.
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
    /// Gets the policy applied to all requests.
    /// </summary>
    public PoliciesCollection BasePolicies { get; internal set; }

    /// <summary>
    /// Gets the application <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ApplicationServices { get; set; } = default!;

    internal IDictionary<string, IOutputCachingPolicy>? NamedPolicies { get; set; }

    /// <summary>
    /// For testing purposes only.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal ISystemClock SystemClock { get; set; } = new SystemClock();

    /// <summary>
    /// Defines a <see cref="IOutputCachingPolicy"/> which can be referenced by name.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="policy">The policy to add</param>
    public void AddPolicy(string name, IOutputCachingPolicy policy)
    {
        NamedPolicies ??= new Dictionary<string, IOutputCachingPolicy>(StringComparer.OrdinalIgnoreCase);
        NamedPolicies[name] = policy;
    }

    /// <summary>
    /// Defines a <see cref="IOutputCachingPolicy"/> which can be referenced by name.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="build">an action on <see cref="OutputCachePolicyBuilder"/>.</param>
    public void AddPolicy(string name, Action<OutputCachePolicyBuilder> build)
    {
        var builder = new OutputCachePolicyBuilder(this);
        build(builder);
        NamedPolicies ??= new Dictionary<string, IOutputCachingPolicy>(StringComparer.OrdinalIgnoreCase);
        NamedPolicies[name] = builder.Build();
    }
}
