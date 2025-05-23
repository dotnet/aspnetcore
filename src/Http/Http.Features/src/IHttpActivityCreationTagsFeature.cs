// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides a mechanism to add tags to the <see cref="System.Diagnostics.Activity"/> at creation time for incoming HTTP requests.
/// These tags can be used for tracing when making sampling decisions.
/// </summary>
public interface IHttpActivityCreationTagsFeature
{
    /// <summary>
    /// A collection of tags to be added to the <see cref="System.Diagnostics.Activity"/> when it is created for the current HTTP request.
    /// These tags are available at Activity creation time and can be used for sampling decisions.
    /// </summary>
    /// <returns>Tags to add to the Activity at creation time.</returns>
    IEnumerable<KeyValuePair<string, object?>>? ActivityCreationTags { get; }
}
