// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Media;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal static class MediaContentFactory
{
    public static RenderFragment Create(DataContent content, string? alternativeText = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        var source = new MediaSource(
            content.Data.ToArray(),
            content.MediaType,
            $"ai-media-{Guid.NewGuid():N}");
        var attributes = new Dictionary<string, object>();

        return builder =>
        {
            if (content.HasTopLevelMediaType("image"))
            {
                attributes["alt"] = alternativeText ?? GetDisplayName(content, "Attached image");
                builder.OpenComponent<Media.Image>(0);
                builder.AddComponentParameter(1, nameof(Media.Image.Source), source);
                builder.AddComponentParameter(2, nameof(Media.Image.AdditionalAttributes), attributes);
                builder.CloseComponent();
            }
            else if (content.HasTopLevelMediaType("audio"))
            {
                attributes.TryAdd("controls", true);
                attributes.TryAdd("preload", "metadata");
                attributes.TryAdd("aria-label", GetDisplayName(content, "Attached audio"));
                builder.OpenComponent<Audio>(0);
                builder.AddComponentParameter(1, nameof(Audio.Source), source);
                builder.AddComponentParameter(2, nameof(Audio.AdditionalAttributes), attributes);
                builder.CloseComponent();
            }
            else if (content.HasTopLevelMediaType("video"))
            {
                attributes.TryAdd("controls", true);
                attributes.TryAdd("preload", "metadata");
                attributes.TryAdd("aria-label", GetDisplayName(content, "Attached video"));
                builder.OpenComponent<Video>(0);
                builder.AddComponentParameter(1, nameof(Video.Source), source);
                builder.AddComponentParameter(2, nameof(Video.AdditionalAttributes), attributes);
                builder.CloseComponent();
            }
            else
            {
                builder.OpenComponent<FileDownload>(0);
                builder.AddComponentParameter(1, nameof(FileDownload.Source), source);
                builder.AddComponentParameter(2, nameof(FileDownload.FileName), GetFileName(content));
                builder.AddComponentParameter(3, nameof(FileDownload.Text), GetDisplayName(content, "Download attachment"));
                builder.AddComponentParameter(4, nameof(FileDownload.AdditionalAttributes), attributes);
                builder.CloseComponent();
            }
        };
    }

    private static string GetDisplayName(DataContent content, string fallback)
    {
        return string.IsNullOrWhiteSpace(content.Name)
            ? fallback
            : content.Name;
    }

    private static string GetFileName(DataContent content)
    {
        return string.IsNullOrWhiteSpace(content.Name)
            ? "attachment"
            : Path.GetFileName(content.Name);
    }
}
