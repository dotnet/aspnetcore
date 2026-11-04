// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

internal static class MetricsExtensions
{
    public static bool TryAddTag(this IHttpMetricsTagsFeature feature, string name, object? value)
    {
        var tags = feature.Tags;

        return TryAddTagCore(name, value, tags);
    }

    public static bool TryAddTag(this IConnectionMetricsTagsFeature feature, string name, object? value)
    {
        var tags = feature.Tags;

        return TryAddTagCore(name, value, tags);
    }

    public static void SetTag(this IConnectionMetricsTagsFeature feature, string name, object? value)
    {
        var tags = feature.Tags;

        SetTagCore(name, value, tags);
    }

    private static void SetTagCore(string name, object? value, ICollection<KeyValuePair<string, object?>> tags)
    {
        // Tags is internally represented as a List<T>.
        // Prefer looping through the list to avoid allocating an enumerator.
        if (tags is List<KeyValuePair<string, object?>> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Key == name)
                {
                    list[i] = new KeyValuePair<string, object?>(name, value);
                    break;
                }
            }
        }
        else
        {
            foreach (var tag in tags)
            {
                if (tag.Key == name)
                {
                    tags.Remove(tag);
                    tags.Add(new KeyValuePair<string, object?>(name, value));
                    break;
                }
            }
        }
    }

    private static bool TryAddTagCore(string name, object? value, ICollection<KeyValuePair<string, object?>> tags)
    {
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
