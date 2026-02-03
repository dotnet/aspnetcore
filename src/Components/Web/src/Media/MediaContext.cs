// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Media;

/// <summary>
/// Base context supplied to media component <see cref="RenderFragment{TContext}"/> custom content.
/// Used by <see cref="Image"/> and <see cref="Video"/>, and as the base for <see cref="FileDownloadContext"/>.
/// </summary>
public class MediaContext
{
    /// <summary>
    /// The object URL for the media (image/video) or <c>null</c> if not loaded yet (not used for FileDownload).
    /// </summary>
    public string? ObjectUrl { get; internal set; }

    /// <summary>
    /// Indicates whether the media is currently loading.
    /// </summary>
    public bool IsLoading { get; internal set; }

    /// <summary>
    /// Indicates whether the last load attempt failed.
    /// </summary>
    public bool HasError { get; internal set; }

    private Action<ElementReference>? _capture;
    private ElementReference _element;

    internal void Initialize(Action<ElementReference> capture) => _capture = capture;

    /// <summary>
    /// Element reference for use with <c>@ref</c>. Assigning this propagates the DOM element to the component.
    /// </summary>
    public ElementReference Element
    {
        get => _element;
        set
        {
            _element = value;
            _capture?.Invoke(value);
        }
    }
}
