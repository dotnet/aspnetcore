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
        return await _httpClient.GetByteArrayAsync($"sample-data/{filename}");
    }
}
