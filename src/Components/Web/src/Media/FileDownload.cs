// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Media;

/* This is equivalent to a .razor file containing:
 *
 * <a data-blazor-file-download
 *    href="javascript:void(0)"
 *    data-state=@(IsLoading ? "loading" : _hasError ? "error" : null)
 *    @attributes="AdditionalAttributes"
 *    @ref="Element"
 *    @onclick="OnClickAsync">@(Text ?? "Download")</a>
 *
 */
/// <summary>
/// A component that provides an anchor element to download the provided media source.
/// </summary>
public sealed class FileDownload : MediaComponentBase
{
    /// <summary>
    /// File name to suggest to the browser for the download. Must be provided.
    /// </summary>
    [Parameter, EditorRequired] public string FileName { get; set; } = default!;

    /// <summary>
    /// Provides custom link text. Defaults to "Download".
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <inheritdoc />
    protected override string TagName => "a";

    /// <inheritdoc />
    protected override string TargetAttributeName => string.Empty; // Not used – object URL not tracked for downloads.

    /// <inheritdoc />
    protected override string MarkerAttributeName => "data-blazor-file-download";

    /// <inheritdoc />
    protected override bool ShouldAutoLoad => false;

    /// <summary>
    /// Builds the anchor element with click handler wiring. The anchor is given an inert href (if one is not supplied)
    /// and the click default action is prevented so navigation does not occur. Styling into a button shape is left to the user.
    /// </summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, TagName);

        builder.AddAttribute(1, MarkerAttributeName, "");

        if (IsLoading)
        {
            builder.AddAttribute(2, "data-state", "loading");
        }
        else if (_hasError)
        {
            builder.AddAttribute(2, "data-state", "error");
        }

        builder.AddAttribute(3, "href", "javascript:void(0)");

        builder.AddAttribute(4, "onclick", EventCallback.Factory.Create(this, OnClickAsync));

        if (AdditionalAttributes?.ContainsKey("href") == true)
        {
            AdditionalAttributes.Remove("href");
        }
        builder.AddMultipleAttributes(6, AdditionalAttributes);

        builder.AddElementReferenceCapture(7, elementReference => Element = elementReference);

        builder.AddContent(8, Text ?? "Download");

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
        _currentSource = Source;
        Render();

        var source = Source;

        using var streamRef = new DotNetStreamReference(source.Stream, leaveOpen: true);

        try
        {
            var result = await JSRuntime.InvokeAsync<bool>(
                "Blazor._internal.BinaryMedia.downloadAsync",
                token,
                Element,
                streamRef,
                source.MimeType,
                source.Length,
                FileName);

            if (!token.IsCancellationRequested)
            {
                _currentSource = null;
                if (!result)
                {
                    _hasError = true;
                }
                Render();
            }
        }
        catch (OperationCanceledException)
        {
            _currentSource = null;
            Render();
        }
        catch
        {
            _currentSource = null;
            _hasError = true;
            Render();
        }
    }
}
