// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Helper for storing links between diagnostic activities in Blazor components.
/// </summary>
public interface IComponentsActivityLinkStore
{
    /// <summary>
    /// Sets the activity context for a specific category.
    /// </summary>
    /// <param name="category">Category of the trace.</param>
    /// <param name="activityLink">Link to the trace.</param>
    /// <param name="tag">Optional tag metadata.</param>
    void SetActivityContext(int category, ActivityContext activityLink, KeyValuePair<string, object?>? tag);

    /// <summary>
    /// Will add all activity contexts except the one specified by <paramref name="exceptCategory"/> to the <paramref name="targetActivity"/>.
    /// </summary>
    /// <param name="exceptCategory">Category of the target trace.</param>
    /// <param name="targetActivity">Activity to add links to.</param>
    void AddActivityContexts(int exceptCategory, Activity targetActivity);
}

/// <summary>
/// Helper for storing links between diagnostic activities in Blazor components.
/// </summary>
public static class ComponentsActivityCategory
{
    /// <summary>
    /// Http trace.
    /// </summary>
    public const int Http = 0;
    /// <summary>
    /// SignalR trace.
    /// </summary>
    public const int SignalR = 1;
    /// <summary>
    /// Route trace.
    /// </summary>
    public const int Route = 2;
    /// <summary>
    /// Circuit trace.
    /// </summary>
    public const int Circuit = 3;

    internal const int COUNT = 4;// keep this one bigger than the last linkable category index

    /// <summary>
    /// Event trace.
    /// </summary>
    internal const int Event = 5; // not linkable
}

internal class ComponentsActivityLinkStore : IComponentsActivityLinkStore
{
    private readonly ActivityContext[] _activityLinks = new ActivityContext[ComponentsActivityCategory.COUNT];
    private readonly KeyValuePair<string, object?>?[] _activityTags = new KeyValuePair<string, object?>?[ComponentsActivityCategory.COUNT];

    public void SetActivityContext(int category, ActivityContext activityLink, KeyValuePair<string, object?>? tag)
    {
        _activityLinks[category] = activityLink;
        _activityTags[category] = tag;
    }

    public void AddActivityContexts(int exceptCategory, Activity targetActivity)
    {
        for (var i = 0; i < ComponentsActivityCategory.COUNT; i++)
        {
            if (i != exceptCategory)
            {
                if (_activityLinks[i] != default)
                {
                    targetActivity.AddLink(new ActivityLink(_activityLinks[i]));
                }
                var tag = _activityTags[i];
                if (tag != null)
                {
                    targetActivity.SetTag(tag.Value.Key, tag.Value.Value);
                }
            }
        }
    }
}
