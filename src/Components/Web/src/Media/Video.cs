// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web.Media;

/* This is equivalent to a .razor file containing:
 *
 * <video data-blazor-video
 *        src="@(_currentObjectUrl)"
 *        data-state=@(IsLoading ? "loading" : _hasError ? "error" : null)
 *        @attributes="AdditionalAttributes"
 *        @ref="Element"></video>
 *
 */
/// <summary>
/// A component that efficiently renders video content from non-HTTP sources like byte arrays.
/// </summary>
public sealed class Video : MediaComponentBase
{
    internal override string TargetAttributeName => "src";

    /// <summary>
    /// Allows customizing the rendering of the video component.
    /// </summary>
    [Parameter] public RenderFragment<MediaContext>? ChildContent { get; set; }

    private protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is not null)
        {
            var showInitial = Source != null && _currentSource == null && string.IsNullOrEmpty(_currentObjectUrl) && !_hasError;
            var context = new MediaContext
            {
                ObjectUrl = _currentObjectUrl,
                IsLoading = IsLoading || showInitial,
                HasError = _hasError,
            };
            context.Initialize(r => Element = r);
            builder.AddContent(0, ChildContent, context);
            return;
        }

        // Default rendering
        builder.OpenElement(0, "video");

        if (!string.IsNullOrEmpty(_currentObjectUrl))
        {
            builder.AddAttribute(1, TargetAttributeName, _currentObjectUrl);
        }

        builder.AddAttribute(2, "data-blazor-video", "");

        var defaultShowInitial = Source != null && _currentSource == null && string.IsNullOrEmpty(_currentObjectUrl) && !_hasError;
        if (IsLoading || defaultShowInitial)
        {
            builder.AddAttribute(3, "data-state", "loading");
        }
        else if (_hasError)
        {
            builder.AddAttribute(3, "data-state", "error");
        }

        builder.AddMultipleAttributes(4, AdditionalAttributes);
        builder.AddElementReferenceCapture(5, r => Element = r);
        builder.CloseElement();
    }
}
