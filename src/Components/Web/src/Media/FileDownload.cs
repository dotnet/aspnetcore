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

    internal override string TargetAttributeName => string.Empty; // Not used – object URL not tracked for downloads.

    /// <inheritdoc />
    internal override bool ShouldAutoLoad => false;

    /// <summary>
    /// Allows customizing the rendering of the file download component.
    /// </summary>
    [Parameter] public RenderFragment<FileDownloadContext>? ChildContent { get; set; }

    private protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is not null)
        {
            var context = new FileDownloadContext
            {
                IsLoading = IsLoading,
                HasError = _hasError,
                FileName = FileName,
            };
            context.Initialize(r => Element = r, EventCallback.Factory.Create(this, OnClickAsync));
            builder.AddContent(0, ChildContent, context);
            return;
        }

        // Default rendering
        builder.OpenElement(0, "a");

        builder.AddAttribute(1, "data-blazor-file-download", "");

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

        IEnumerable<KeyValuePair<string, object>>? attributesToRender = AdditionalAttributes;
        if (AdditionalAttributes is not null && AdditionalAttributes.ContainsKey("href"))
        {
            var copy = new Dictionary<string, object>(AdditionalAttributes.Count);
            foreach (var kvp in AdditionalAttributes)
            {
                if (kvp.Key == "href")
                {
                    continue;
                }
                copy.Add(kvp.Key, kvp.Value!);
            }
            attributesToRender = copy;
        }
        builder.AddMultipleAttributes(6, attributesToRender);

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

/// <summary>
/// Extended media context for the FileDownload component providing click invocation and filename.
/// </summary>
public sealed class FileDownloadContext : MediaContext
{
    /// <summary>
    /// Gets the file name suggested to the browser when initiating the download.
    /// </summary>
    public string FileName { get; internal set; } = string.Empty;
    private EventCallback _onClick;
    internal void Initialize(Action<ElementReference> capture, EventCallback onClick)
    {
        base.Initialize(capture);
        _onClick = onClick;
    }
    /// <summary>
    /// Initiates the download by invoking the underlying click handler of the parent.
    /// </summary>
    public Task InvokeAsync() => _onClick.InvokeAsync();
}
