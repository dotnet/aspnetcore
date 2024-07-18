// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class HttpMetricsTagsFeature : IHttpMetricsTagsFeature
{
    ICollection<KeyValuePair<string, object?>> IHttpMetricsTagsFeature.Tags => TagsList;
    public bool MetricsDisabled { get; set; }

    public List<KeyValuePair<string, object?>> TagsList { get; } = new List<KeyValuePair<string, object?>>();

    // Cache request values when request starts. These are used when writing metrics when the request ends.
    // This ensures that the tags match between the start and end of the request. Important for up/down counters.
    public string? Method { get; set; }
    public string? Scheme { get; set; }
    public string? Protocol { get; set; }

    public void Reset()
    {
        TagsList.Clear();
        MetricsDisabled = false;

        Method = null;
        Scheme = null;
        Protocol = null;
    }
}
