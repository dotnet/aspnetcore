// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides access to tags added to the metrics HTTP request counter. This feature isn't set if the counter isn't enabled.
/// </summary>
public interface IHttpMetricsTagsFeature
{
    /// <summary>
    /// Gets the tag collection.
    /// </summary>
    ICollection<KeyValuePair<string, object?>> Tags { get; }
}
