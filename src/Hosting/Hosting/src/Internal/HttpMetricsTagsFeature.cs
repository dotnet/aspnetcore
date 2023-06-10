// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class HttpMetricsTagsFeature : IHttpMetricsTagsFeature
{
    ICollection<KeyValuePair<string, object?>> IHttpMetricsTagsFeature.Tags => TagsList;

    public List<KeyValuePair<string, object?>> TagsList { get; } = new List<KeyValuePair<string, object?>>();
}
