// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides access to tags added to HTTP request duration metrics. This feature isn't set if the counter isn't enabled.
/// </summary>
public interface IHttpMetricsTagsFeature
{
    /// <summary>
    /// Gets the tag collection.
    /// </summary>
    ICollection<KeyValuePair<string, object?>> Tags { get; }

    // MetricsDisabled was added after the initial release of this interface and is intentionally a DIM property.
    /// <summary>
    /// Gets or sets a flag that disables recording HTTP request duration metrics for the current HTTP request.
    /// </summary>
    public bool MetricsDisabled { get; set; }
}
