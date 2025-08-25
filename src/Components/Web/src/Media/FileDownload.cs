// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Media;

/// <summary>
/// A component that provides a button which, when clicked, downloads the provided media source
/// either via a save-as dialog or directly, using the same BinaryMedia pipeline as <see cref="Image"/> and <see cref="Video"/>.
/// </summary>
/// <remarks>
/// Unlike <see cref="Image"/> and <see cref="Video"/>, this component does not automatically load the media.
/// It defers loading until the user clicks the button. The stream is then materialized, optionally cached,
/// and a client-side download is triggered.
/// </remarks>
public sealed class FileDownload : MediaComponentBase
{
    /// <summary>
    /// File name to suggest to the browser for the download. Must be provided.
    /// </summary>
    [Parameter, EditorRequired] public string FileName { get; set; } = default!;

    /// <summary>
    /// Provides custom button text. Defaults to "Download".
    /// </summary>
    [Parameter] public string? ButtonText { get; set; }

    /// <inheritdoc />
    protected override string TagName => "button";

    /// <inheritdoc />
    protected override string TargetAttributeName => string.Empty; // Not used – object URL not tracked for downloads.

    /// <inheritdoc />
    protected override string MarkerAttributeName => "data-blazor-file-download";

    /// <inheritdoc />
    protected override bool ShouldAutoLoad => false;

    /// <summary>
    /// Builds the button element with click handler wiring.
    /// </summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, TagName);

        // Removed object URL attribute emission; not needed for downloads.
        builder.AddAttribute(1, MarkerAttributeName, "");

        if (IsLoading)
        {
            builder.AddAttribute(2, "data-state", "loading");
        }
        else if (_hasError)
        {
            builder.AddAttribute(2, "data-state", "error");
        }

        builder.AddAttribute(3, "type", "button");
        builder.AddAttribute(4, "onclick", EventCallback.Factory.Create(this, OnClickAsync));
        builder.AddMultipleAttributes(5, AdditionalAttributes);
        builder.AddElementReferenceCapture(6, elementReference => Element = elementReference);

        builder.AddContent(7, ButtonText ?? "Download");

        builder.CloseElement();
    }

    private async Task OnClickAsync()
    {
        if (Source is null || !IsInteractive || string.IsNullOrWhiteSpace(FileName))
        {
            return;
        }

        CancelPreviousLoad();
        var token = ResetCancellationToken();
        _hasError = false;

        var source = Source;

        using var streamRef = new DotNetStreamReference(source.Stream, leaveOpen: true);

        try
        {
            var result = await JSRuntime.InvokeAsync<Boolean>(
                "Blazor._internal.BinaryMedia.downloadAsync",
                token,
                Element,
                streamRef,
                source.MimeType,
                source.Length,
                FileName);

            if (result && !token.IsCancellationRequested)
            {
                if (!result)
                {
                    _hasError = true;
                }
                Render();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            _hasError = true;
            Render();
        }
    }
}
