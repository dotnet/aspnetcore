// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Configures options for generated URLs.
/// </summary>
public class LinkOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether all generated paths URLs are lowercase.
    /// Use <see cref="LowercaseQueryStrings" /> to configure the behavior for query strings.
    /// </summary>
    public bool? LowercaseUrls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a generated query strings are lowercase.
    /// This property will be false unless <see cref="LowercaseUrls" /> is also <c>true</c>.
    /// </summary>
    public bool? LowercaseQueryStrings { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a trailing slash should be appended to the generated URLs.
    /// </summary>
    public bool? AppendTrailingSlash { get; set; }
}
