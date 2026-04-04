// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.Test;

public static class MetricsHelpers
{
    public static void AssertHasDurationAndContainsTags(double duration, IReadOnlyDictionary<string, object> tags, List<KeyValuePair<string, object>> expectedTags)
    {
        Assert.True(duration > 0, "Duration should be greater than 0.");
        AssertContainsTags(tags, expectedTags);
    }

    public static void AssertContainsTags(IReadOnlyDictionary<string, object> tags, List<KeyValuePair<string, object>> expectedTags)
    {
        var found = 0;
        foreach (var expectedTag in expectedTags)
        {
            if (tags.TryGetValue(expectedTag.Key, out var value) && EqualityComparer<object>.Default.Equals(value, expectedTag.Value))
            {
                found++;
            }
        }
        if (found != expectedTags.Count)
        {
            throw new InvalidOperationException(
                $"""
                Expected: {string.Join(", ", expectedTags.OrderBy(t => t.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"))}
                Actual: {string.Join(", ", tags.OrderBy(t => t.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"))}
                """);
        }
    }
}
