// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

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
    /// Optional file name to suggest to the browser for the download.
    /// </summary>
    [Parameter] public string? FileName { get; set; }

    /// <summary>
    /// Provides custom button text. Defaults to "Download".
    /// </summary>
    [Parameter] public string? ButtonText { get; set; }

    /// <inheritdoc />
    protected override string TagName => "button"; // Render a button element

    /// <inheritdoc />
    protected override string TargetAttributeName => "data-download-object-url"; // Not an actual browser attribute; used for diagnostics.

    /// <inheritdoc />
    protected override string MarkerAttributeName => "data-blazor-file-download";

    /// <inheritdoc />
    protected override bool ShouldAutoLoad => false; // Manual trigger via click.

    /// <summary>
    /// Builds the button element with click handler wiring.
    /// </summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, TagName);

        if (!string.IsNullOrEmpty(_currentObjectUrl))
        {
            builder.AddAttribute(1, TargetAttributeName, _currentObjectUrl);
        }

        builder.AddAttribute(2, MarkerAttributeName, "");

        if (IsLoading)
        {
            builder.AddAttribute(3, "data-state", "loading");
        }
        else if (_hasError)
        {
            builder.AddAttribute(3, "data-state", "error");
        }

        builder.AddAttribute(4, "type", "button");
        builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, OnClickAsync));
        builder.AddMultipleAttributes(6, AdditionalAttributes);
        builder.AddElementReferenceCapture(7, er => Element = er);

        builder.AddContent(8, ButtonText ?? "Download");

        builder.CloseElement();
    }

    private async Task OnClickAsync()
    {
        if (Source is null || !IsInteractive)
        {
            return;
        }

        // Cancel any existing load
        CancelPreviousLoad();
        var token = ResetCancellationToken();
        _currentSource = Source;
        _hasError = false;
        _currentObjectUrl = null; // Always recreate for downloads
        RequestRender();

        var source = Source;

        try
        {
            var result = await InvokeBinaryMediaAsync(
                "Blazor._internal.BinaryMedia", // JS method we will add
                source,
                token,
                FileName);

            if (result is not null && _activeCacheKey == source.CacheKey && !token.IsCancellationRequested)
            {
                if (result.Success)
                {
                    _currentObjectUrl = result.ObjectUrl; // store created object URL for potential diagnostics
                }
                else
                {
                    _hasError = true;
                }
                RequestRender();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            _hasError = true;
            RequestRender();
        }
    }
}
