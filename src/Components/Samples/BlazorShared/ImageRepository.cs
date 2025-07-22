// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace BlazorShared;

public abstract class ImageRepository
{
    protected readonly ILogger<ImageRepository> _logger;

    protected ImageRepository(ILogger<ImageRepository> logger)
    {
        _logger = logger;
    }

    protected readonly Dictionary<int, ImageMetadata> _metadata = new Dictionary<int, ImageMetadata>();
    protected readonly Dictionary<int, byte[]> _imageData = new Dictionary<int, byte[]>();
    protected int _nextId = 1;
    protected readonly SemaphoreSlim _initializationSemaphore = new SemaphoreSlim(1, 1);
    protected bool _isInitialized = false;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _initializationSemaphore.WaitAsync();
        try
        {
            if (_isInitialized)
            {
                return;
            }

            await AddSampleImageAsync(
                "Curious Cat",
                "A curious cat looking at the camera",
                "cat-1.jpg",
                "image/jpeg"
            );

            await AddSampleImageAsync(
                "Sleepy Cat",
                "Cat taking a nap in a comfortable spot",
                "cat-3.jpg",
                "image/jpeg"
            );

            await AddSampleImageAsync(
                "Alert Cat",
                "Cat with alert expression watching something",
                "cat-2.jpg",
                "image/jpeg"
            );

            await AddSampleImageAsync(
                "Happy Cat",
                "A happy cat enjoying sunshine",
                "cat-4.jpg",
                "image/jpeg"
            );

            await AddSampleImageAsync(
                "High-Resolution Cat",
                "Ultra high-resolution cat image (large file)",
                "cat-5.png",
                "image/png"
            );

            _isInitialized = true;
            _logger.LogInformation("Demo repository initialized with {Count} sample images", _metadata.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize demo repository");
            throw;
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    protected abstract Task<byte[]> LoadImageBytesAsync(string filename);

    protected async Task AddSampleImageAsync(
        string title,
        string description,
        string filename,
        string mimeType)
    {
        try
        {
            var imageBytes = await LoadImageBytesAsync(filename);

            int width = 800;
            int height = 600;

            var id = _nextId++;

            _metadata[id] = new ImageMetadata
            {
                Id = id,
                Title = title,
                Description = description,
                MimeType = mimeType,
                Width = width,
                Height = height,
                FileSize = imageBytes.Length
            };

            _imageData[id] = imageBytes;

            _logger.LogInformation("Added sample image: {Title}, Size: {Size} bytes", title, imageBytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add sample image: {Filename}", filename);
        }
    }

    public Task<List<ImageMetadata>> GetAllImagesMetadataAsync()
    {
        var result = _metadata.Values
            .OrderByDescending(i => i.Id)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<ImageMetadata?> GetImageMetadataAsync(int id)
    {
        if (_metadata.TryGetValue(id, out var metadata))
        {
            return Task.FromResult<ImageMetadata?>(metadata);
        }

        return Task.FromResult<ImageMetadata?>(null);
    }

    public Task<byte[]?> GetImageBytesAsync(int id)
    {
        if (_imageData.TryGetValue(id, out var bytes))
        {
            return Task.FromResult<byte[]?>(bytes);
        }

        return Task.FromResult<byte[]?>(null);
    }

    public Task<bool> DeleteImageAsync(int id)
    {
        var result = _metadata.Remove(id);
        if (result)
        {
            _imageData.Remove(id);
            _logger.LogInformation("Deleted image with ID: {Id}", id);
        }

        return Task.FromResult(result);
    }
}
