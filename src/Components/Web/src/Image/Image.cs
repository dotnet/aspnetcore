// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Image;

/* This is equivalent to a .razor file containing:
 *
 * <div class="blazor-image-container" style="@ContainerStyle">
 *     @if (_isLoading && LoadingContent != null)
 *     {
 *         @LoadingContent
 *     }
 *     else if (_hasError && ErrorContent != null)
 *     {
 *         @ErrorContent
 *     }
 *     <img id="@Id" class="@(_isLoading || _hasError ? "d-none" : GetCssClass())"
 *          alt="@Alt" @ref="Element" @attributes="AdditionalAttributes" />
 * </div>
 */
/// <summary>
/// A component that efficiently renders images from non-HTTP sources like byte arrays.
/// </summary>
public class Image : ComponentBase, IAsyncDisposable
{
    private readonly string _id = $"image-{Guid.NewGuid():N}";
    private bool _isLoading = true;
    private bool _hasError;
    private bool _isDisposed;
    private string? _imageEndpointUrl;
    private bool _useImageEndpoint;

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
    /// Gets the injected <see cref="HttpClient"/>.
    /// </summary>
    [Inject] protected HttpClient HttpClient { get; set; } = default!;

    /// <summary>
    /// Gets or sets the source for the image.
    /// </summary>
    [Parameter] public IImageSource? Source { get; set; }

    /// <summary>
    /// Gets or sets the alt text for the image.
    /// </summary>
    [Parameter] public string? Alt { get; set; }

    /// <summary>
    /// Gets or sets the content to display while the image is loading.
    /// </summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>
    /// Gets or sets the content to display when an error occurs loading the image.
    /// </summary>
    [Parameter] public RenderFragment? ErrorContent { get; set; }

    /// <summary>
    /// Gets or sets the caching strategy for the image.
    /// </summary>
    [Parameter] public CacheStrategy CacheStrategy { get; set; } = CacheStrategy.Memory;

    /// <summary>
    /// Gets or sets whether to use the HTTP endpoint approach for image delivery.
    /// </summary>
    [Parameter] public bool UseImageEndpoint { get; set; }

    /// <summary>
    /// Event callback invoked when the image has loaded successfully.
    /// </summary>
    [Parameter] public EventCallback<bool> OnImageLoaded { get; set; }

    /// <summary>
    /// Event callback invoked when an error occurs loading the image.
    /// </summary>
    [Parameter] public EventCallback<string> OnImageError { get; set; }

    /// <summary>
    /// Gets or sets the attributes for the image.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets or sets the size of the chunks used when sending image data.
    /// </summary>
    [Parameter] public int ChunkSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Event callback invoked to report the progress of image loading.
    /// </summary>
    [Parameter] public EventCallback<double> OnProgress { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Set default content if not provided
        LoadingContent ??= CreateDefaultLoadingContent();
        ErrorContent ??= CreateDefaultErrorContent();
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        _useImageEndpoint = UseImageEndpoint || (!RendererInfo.IsInteractive && Source != null);

        if (_useImageEndpoint)
        {
            await RegisterWithImageEndpoint();
            Console.WriteLine($"Image registered with endpoint: {_imageEndpointUrl}");
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "blazor-image-container");

        string containerStyle = GetContainerStyle();
        if (!string.IsNullOrEmpty(containerStyle))
        {
            builder.AddAttribute(2, "style", containerStyle);
        }

        if (_isLoading && LoadingContent != null)
        {
            builder.AddContent(3, LoadingContent);
        }
        else if (_hasError && ErrorContent != null)
        {
            builder.AddContent(4, ErrorContent);
        }

        builder.OpenElement(5, "img");
        builder.AddAttribute(6, "id", _id);

        var cssClass = _isLoading || _hasError ? "d-none" : GetCssClass();
        if (!string.IsNullOrEmpty(cssClass))
        {
            builder.AddAttribute(7, "class", cssClass);
        }

        if (!string.IsNullOrEmpty(Alt))
        {
            builder.AddAttribute(8, "alt", Alt);
        }

        if (!string.IsNullOrEmpty(_imageEndpointUrl))
        {
            builder.AddAttribute(11, "src", _imageEndpointUrl);
        }

        builder.AddMultipleAttributes(9, AdditionalAttributes);
        builder.AddElementReferenceCapture(10, inputReference => Element = inputReference);

        builder.CloseElement();
        builder.CloseElement();
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_isDisposed && !_useImageEndpoint)
        {
            await LoadImageIfSourceProvided();
        }
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
    }

    private async Task LoadImageIfSourceProvided()
    {
        if (Source == null)
        {
            return;
        }

        try
        {
            SetLoadingState();

            // Check if image is already cached before transferring data
            string? cacheKey = null;
            if (Source is ILoadableImageSource loadableSource)
            {
                cacheKey = loadableSource.CacheKey;
            }
            else if (Source is IStreamingImageSource streamingSource)
            {
                cacheKey = streamingSource.CacheKey;
            }

            if (!string.IsNullOrEmpty(cacheKey))
            {
                bool foundInCache = await JSRuntime.InvokeAsync<bool>(
                    "Blazor._internal.BinaryImageComponent.trySetFromCache",
                    _id, cacheKey);

                if (foundInCache)
                {
                    await SetSuccessState();
                    return;
                }
            }

            // If not in cache, proceed with transfer
            if (Source is IStreamingImageSource streamingSource2)
            {
                await StreamImageInChunks(streamingSource2);
            }
            else if (Source is ILoadableImageSource loadableSource2)
            {
                byte[] imageData = await loadableSource2.GetBytesAsync();
                await SendImageInChunks(imageData, loadableSource2.MimeType, loadableSource2.CacheKey);
            }
            else
            {
                throw new InvalidOperationException(
                    "The provided image source must be either ILoadableImageSource or IStreamingImageSource.");
            }

            await SetSuccessState();
        }
        catch (Exception ex)
        {
            await SetErrorState(ex.Message);
        }
    }

    private async Task SendImageInChunks(byte[] imageData, string mimeType, string cacheKey)
    {
        try
        {
            int totalChunks = (int)Math.Ceiling((double)imageData.Length / ChunkSize);
            string transferId = $"{_id}-{Guid.NewGuid():N}";

            byte[] chunkBuffer = ArrayPool<byte>.Shared.Rent(ChunkSize);

            await JSRuntime.InvokeVoidAsync(
                "Blazor._internal.BinaryImageComponent.initChunkedTransfer",
                _id, transferId, totalChunks, imageData.Length, mimeType, cacheKey,
                CacheStrategy.ToString().ToLowerInvariant());
            try
            {
                for (int i = 0; i < totalChunks; i++)
                {
                    int offset = i * ChunkSize;
                    int length = Math.Min(ChunkSize, imageData.Length - offset);

                    Array.Copy(imageData, offset, chunkBuffer, 0, length);

                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.addChunk",
                        transferId, i, chunkBuffer.AsMemory(0, length).ToArray());

                    double progress = (i + 1) / (double)totalChunks;
                    await OnProgress.InvokeAsync(progress);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(chunkBuffer);
            }

            await JSRuntime.InvokeVoidAsync(
                    "Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer",
                    transferId, _id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send chunked image data: {ex.Message}", ex);
        }
    }

    private async Task StreamImageInChunks(IStreamingImageSource source)
    {
        try
        {
            long totalSize = await source.GetSizeAsync();
            int totalChunks = (int)Math.Ceiling((double)totalSize / ChunkSize);
            string transferId = $"{_id}-{Guid.NewGuid():N}";

            await JSRuntime.InvokeVoidAsync(
                "Blazor._internal.BinaryImageComponent.initChunkedTransfer",
                _id, transferId, totalChunks, totalSize, source.MimeType, source.CacheKey,
                CacheStrategy.ToString().ToLowerInvariant());

            byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
            try
            {
                using Stream stream = await source.OpenReadStreamAsync();
                int chunkIndex = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, ChunkSize))) > 0)
                {
                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.addChunk",
                        transferId, chunkIndex, buffer.AsMemory(0, bytesRead).ToArray());

                    double progress = (chunkIndex + 1) / (double)totalChunks;
                    await OnProgress.InvokeAsync(progress);

                    chunkIndex++;
                }

                await JSRuntime.InvokeVoidAsync(
                    "Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer",
                    transferId, _id);
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

    private async Task RegisterWithImageEndpoint()
    {
        if (Source == null)
        {
            return;
        }

        try
        {
            byte[] imageData;
            string contentType;

            if (Source is ILoadableImageSource loadable)
            {
                imageData = await loadable.GetBytesAsync();
                contentType = loadable.MimeType ?? "application/octet-stream";
            }
            else if (Source is IStreamingImageSource streaming)
            {
                using var stream = await streaming.OpenReadStreamAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                imageData = memoryStream.ToArray();
                contentType = streaming.MimeType ?? "application/octet-stream";
            }
            else
            {
                throw new InvalidOperationException(
                    "The provided image source must be either ILoadableImageSource or IStreamingImageSource.");
            }

            var requestContent = new ImageRegistrationRequest(imageData, contentType);

            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await HttpClient.PostAsJsonAsync("_blazor/image/register",
                requestContent,
                ImageJsonSerializerContext.Default.ImageRegistrationRequest,
                tokenSource.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(ImageJsonSerializerContext.Default.ImageRegistrationResponse,
                   tokenSource.Token);
                _imageEndpointUrl = result?.Url;
                await SetSuccessState();
            }
            else
            {
                await SetErrorState($"Failed to register image: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            await SetErrorState($"Error registering image: {ex.Message}");
        }
    }

    private void SetLoadingState()
    {
        _isLoading = true;
        _hasError = false;
        StateHasChanged();
    }

    private async Task SetSuccessState()
    {
        _isLoading = false;
        _hasError = false;
        StateHasChanged();
        await OnImageLoaded.InvokeAsync(true);
    }

    private async Task SetErrorState(string error)
    {
        _isLoading = false;
        _hasError = true;
        StateHasChanged();
        await OnImageError.InvokeAsync(error);
    }

    private static RenderFragment CreateDefaultLoadingContent() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "blazor-image-loading");
        builder.AddAttribute(2, "style", "display: flex; justify-content: center; align-items: center; padding: 20px;");

        builder.OpenElement(3, "div");
        builder.AddAttribute(4, "class", "loading-spinner");
        builder.AddAttribute(5, "style", @"
            border: 4px solid #f3f3f3;
            border-top: 4px solid #3498db;
            border-radius: 50%;
            width: 40px;
            height: 40px;
            animation: blazor-image-spin 1s linear infinite;
            margin: 0 auto;
        ");
        builder.CloseElement();

        builder.OpenElement(6, "style");
        builder.AddContent(7, @"
            @keyframes blazor-image-spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
        ");
        builder.CloseElement();

        builder.CloseElement();
    };

    private static RenderFragment CreateDefaultErrorContent() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "blazor-image-error");
        builder.AddAttribute(2, "style", "display: flex; justify-content: center; align-items: center; padding: 20px; flex-direction: column; text-align: center;");

        builder.OpenElement(3, "span");
        builder.AddAttribute(4, "class", "error-icon");
        builder.AddAttribute(5, "style", "font-size: 24px; margin-bottom: 8px;");
        builder.AddContent(6, "⚠️");
        builder.CloseElement();

        builder.OpenElement(7, "span");
        builder.AddAttribute(8, "class", "error-message");
        builder.AddAttribute(9, "style", "color: #dc3545; font-size: 14px;");
        builder.AddContent(10, "Failed to load image");
        builder.CloseElement();

        builder.CloseElement();
    };

    private string GetContainerStyle() => AdditionalAttributes?.TryGetValue("style", out var style) == true
        ? style?.ToString() ?? string.Empty : string.Empty;

    private string GetCssClass() => AdditionalAttributes?.TryGetValue("class", out var cssClass) == true
        ? cssClass?.ToString() ?? string.Empty : string.Empty;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (Source != null && RendererInfo.IsInteractive == true && string.IsNullOrEmpty(_imageEndpointUrl))
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.revokeImageUrl",
                        _id);
                }
                catch (JSDisconnectedException)
                {
                    // Client disconnected
                }
            }
        }
    }

    internal class ImageRegistrationResponse
    {
        public string? Url { get; set; }
    }

    internal class ImageRegistrationRequest
    {
        public byte[]? ImageData { get; set; }

        public string? ContentType { get; set; }

        public ImageRegistrationRequest(byte[] imageData, string contentType)
        {
            ImageData = imageData;
            ContentType = contentType;
        }

        public ImageRegistrationRequest() { }
    }

}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(Image.ImageRegistrationRequest))]
[JsonSerializable(typeof(Image.ImageRegistrationResponse))]
internal partial class ImageJsonSerializerContext : JsonSerializerContext
{
}
