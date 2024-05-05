// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

internal static class MetricsExtensions
{
    public static bool TryAddTag(this IHttpMetricsTagsFeature feature, string name, object? value)
    {
        var tags = feature.Tags;

        // Tags is internally represented as a List<T>.
        // Prefer looping through the list to avoid allocating an enumerator.
        if (tags is List<KeyValuePair<string, object?>> list)
        {
            foreach (var tag in list)
            {
                if (tag.Key == name)
                {
                    return false;
                }
            }
        }
        else
        {
            foreach (var tag in tags)
            {
                if (tag.Key == name)
                {
                    return false;
                }
            }
        }

        tags.Add(new KeyValuePair<string, object?>(name, value));
        return true;
    }

    public static bool TryAddTag(this ref TagList tags, string name, object? value)
    {
        for (var i = 0; i < tags.Count; i++)
        {
            if (tags[i].Key == name)
            {
                return false;
            }
        }

        tags.Add(new KeyValuePair<string, object?>(name, value));
        return true;
    }
}
