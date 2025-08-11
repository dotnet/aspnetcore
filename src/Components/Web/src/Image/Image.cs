// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Image;

/* This is equivalent to a .razor file containing:
 *
 * <img class="blazor-image @GetCssClass()"
 *      data-state="@(_isLoading ? "loading" : _hasError ? "error" : null)"
 *      @ref="Element" @attributes="AdditionalAttributes" />
 */
/// <summary>
/// A component that efficiently renders images from non-HTTP sources like byte arrays.
/// </summary>
public class Image : ComponentBase, IAsyncDisposable
{
    private bool _isLoading = true;
    private bool _hasError;
    private bool _isDisposed;
    // Version counter to guard against stale concurrent loads.
    private int _loadVersion;

    /// <summary>
    /// Gets or sets the associated <see cref="ElementReference"/>.
    /// <para>
    /// May be <see langword="null"/> if accessed before the component is rendered.
    /// </para>
    /// </summary>
    [DisallowNull] public ElementReference? Element { get; protected set; }

    /// <summary>
    /// Gets the injected <see cref="IJSRuntime"/>.
    /// </summary>
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets the source for the image.
    /// </summary>
    [Parameter] public ImageSource? Source { get; set; }

    /// <summary>
    /// Gets or sets the caching strategy for the image.
    /// </summary>
    [Parameter] public CacheStrategy CacheStrategy { get; set; } = CacheStrategy.Memory;

    /// <summary>
    /// Gets or sets the attributes for the image.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets or sets the size of the chunks used when sending image data.
    /// </summary>
    [Parameter] public int ChunkSize { get; set; } = 64 * 1024;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "img");

        if (_isLoading)
        {
            builder.AddAttribute(1, "data-state", "loading");
        }
        else if (_hasError)
        {
            builder.AddAttribute(1, "data-state", "error");
        }

        var cssClass = GetCssClass();
        builder.AddAttribute(3, "class", $"blazor-image {cssClass}".Trim());

        builder.AddMultipleAttributes(4, AdditionalAttributes);
        builder.AddElementReferenceCapture(5, elementReference => Element = elementReference);

        builder.CloseElement();
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_isDisposed)
        {
            await LoadImageIfSourceProvided(_loadVersion, Source);
        }
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters) // OnParametersAsync
    {
        var previousSource = Source;

        await base.SetParametersAsync(parameters);

        if (previousSource?.CacheKey != Source?.CacheKey)
        {
            if (Source != null && !_isDisposed)
            {
                var version = ++_loadVersion;

                try
                {
                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.revokeImageUrl",
                        Element);
                }
                catch (JSDisconnectedException) { }
                catch (JSException) { }

                await LoadImageIfSourceProvided(version, Source);
            }
        }
    }

    // Guarded load: only the invocation whose version matches _loadVersion at completion updates state.
    private async Task LoadImageIfSourceProvided(int version, ImageSource? source)
    {
        if (source == null)
        {
            return;
        }

        try
        {
            SetLoadingState();

            // Check cache first
            if (!string.IsNullOrEmpty(source.CacheKey))
            {
                bool foundInCache = await JSRuntime.InvokeAsync<bool>(
                    "Blazor._internal.BinaryImageComponent.trySetFromCache",
                    Element, source.CacheKey);

                if (foundInCache)
                {
                    if (version == _loadVersion)
                    {
                        SetSuccessState();
                    }
                    return;
                }
            }

            await StreamImageInChunks(source, version);

            if (version == _loadVersion)
            {
                SetSuccessState();
            }
        }
        catch (Exception)
        {
            if (version == _loadVersion)
            {
                SetErrorState();
            }
        }
    }

    private async Task StreamImageInChunks(ImageSource source, int version)
    {
        if (version != _loadVersion)
        {
            return;
        }

        try
        {
            string transferId = $"transfer-{Guid.NewGuid():N}";

            await JSRuntime.InvokeVoidAsync(
                "Blazor._internal.BinaryImageComponent.initChunkedTransfer",
                Element, transferId, source.MimeType, source.CacheKey,
                CacheStrategy.ToString().ToLowerInvariant(), source.Length);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
            try
            {
                var stream = source.Stream;

                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, ChunkSize))) > 0)
                {
                    if (version != _loadVersion)
                    {
                        return;
                    }

                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.addChunk",
                        transferId, buffer.AsMemory(0, bytesRead).ToArray());
                }

                if (version == _loadVersion)
                {
                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer",
                        transferId);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to stream image data: {ex.Message}", ex);
        }
    }

    private void SetLoadingState()
    {
        _isLoading = true;
        _hasError = false;
        StateHasChanged();
    }

    private void SetSuccessState()
    {
        _isLoading = false;
        _hasError = false;
        StateHasChanged();
    }

    private void SetErrorState()
    {
        _isLoading = false;
        _hasError = true;
        StateHasChanged();
    }

    private string GetCssClass() => AdditionalAttributes?.TryGetValue("class", out var cssClass) == true
        ? cssClass?.ToString() ?? string.Empty : string.Empty;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (Source != null && RendererInfo.IsInteractive == true)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.revokeImageUrl",
                        Element);
                }
                catch (JSDisconnectedException)
                {
                    // Client disconnected
                }
            }
        }
    }
}
