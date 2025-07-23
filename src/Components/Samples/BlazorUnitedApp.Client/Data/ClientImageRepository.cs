// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using BlazorShared;

namespace BlazorUnitedApp.Client.Data;

public class ClientImageRepository : ImageRepository
{
    private readonly HttpClient _httpClient;

    public ClientImageRepository(HttpClient httpClient, ILogger<ImageRepository> logger)
        : base(logger)
    {
        _httpClient = httpClient;
    }

    protected override async Task<byte[]> LoadImageBytesAsync(string filename)
    {
        try
        {
            return await _httpClient.GetByteArrayAsync($"sample-data/{filename}");
        }
        catch (HttpRequestException ex)
        {
            // During prerendering, HTTP calls may fail - log and return empty array
            _logger.LogWarning("Failed to load image {Filename} during prerendering: {Error}", filename, ex.Message);

            // Return a minimal placeholder image or empty array
            // This prevents the prerendering from failing while still showing the component structure
            return Array.Empty<byte>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning("Timeout loading image {Filename} during prerendering: {Error}", filename, ex.Message);
            return Array.Empty<byte>();
        }
    }
}
