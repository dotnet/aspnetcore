// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Media;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Renders binary AI content using the matching component from
/// <see cref="Microsoft.AspNetCore.Components.Media"/>.
/// </summary>
public sealed class MediaContent : ComponentBase
{
    private static readonly ConditionalWeakTable<DataContent, MediaCacheKey> CacheKeys = new();
    private DataContent? _currentContent;
    private MediaSource? _source;

    /// <summary>
    /// Gets or sets the content to render.
    /// </summary>
    [Parameter, EditorRequired]
    public DataContent Content { get; set; } = default!;

    /// <summary>
    /// Gets or sets the accessible alternative text for image content.
    /// </summary>
    [Parameter]
    public string? AlternativeText { get; set; }

    /// <summary>
    /// Gets or sets additional attributes applied to the rendered media element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Content);

        if (!ReferenceEquals(_currentContent, Content))
        {
            _currentContent = Content;
            _source = new MediaSource(
                Content.Data.ToArray(),
                Content.MediaType,
                CacheKeys.GetValue(
                    Content,
                    static _ => new MediaCacheKey($"ai-media-{Guid.NewGuid():N}")).Value);
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var source = _source
            ?? throw new InvalidOperationException($"{nameof(MediaContent)}.{nameof(Content)} is required.");
        var attributes = CreateMediaAttributes();

        if (Content.HasTopLevelMediaType("image"))
        {
            attributes["alt"] = AlternativeText ?? GetDisplayName("Attached image");
            builder.OpenComponent<Media.Image>(0);
            builder.AddComponentParameter(1, nameof(Media.Image.Source), source);
            builder.AddComponentParameter(2, nameof(Media.Image.AdditionalAttributes), attributes);
            builder.CloseComponent();
        }
        else if (Content.HasTopLevelMediaType("audio"))
        {
            attributes.TryAdd("controls", true);
            attributes.TryAdd("preload", "metadata");
            attributes.TryAdd("aria-label", GetDisplayName("Attached audio"));
            builder.OpenComponent<Audio>(0);
            builder.AddComponentParameter(1, nameof(Audio.Source), source);
            builder.AddComponentParameter(2, nameof(Audio.AdditionalAttributes), attributes);
            builder.CloseComponent();
        }
        else if (Content.HasTopLevelMediaType("video"))
        {
            attributes.TryAdd("controls", true);
            attributes.TryAdd("preload", "metadata");
            attributes.TryAdd("aria-label", GetDisplayName("Attached video"));
            builder.OpenComponent<Video>(0);
            builder.AddComponentParameter(1, nameof(Video.Source), source);
            builder.AddComponentParameter(2, nameof(Video.AdditionalAttributes), attributes);
            builder.CloseComponent();
        }
        else
        {
            builder.OpenComponent<FileDownload>(0);
            builder.AddComponentParameter(1, nameof(FileDownload.Source), source);
            builder.AddComponentParameter(2, nameof(FileDownload.FileName), GetFileName());
            builder.AddComponentParameter(3, nameof(FileDownload.Text), GetDisplayName("Download attachment"));
            builder.AddComponentParameter(4, nameof(FileDownload.AdditionalAttributes), attributes);
            builder.CloseComponent();
        }
    }

    private Dictionary<string, object> CreateMediaAttributes()
    {
        return AdditionalAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(AdditionalAttributes);
    }

    private string GetDisplayName(string fallback)
    {
        return string.IsNullOrWhiteSpace(Content.Name)
            ? fallback
            : Content.Name;
    }

    private string GetFileName()
    {
        return string.IsNullOrWhiteSpace(Content.Name)
            ? "attachment"
            : Path.GetFileName(Content.Name);
    }

    private sealed record MediaCacheKey(string Value);
}
