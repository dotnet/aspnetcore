// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorShared;

namespace BlazorUnitedApp.Data;

public class ServerImageRepository : ImageRepository
{
    private readonly IWebHostEnvironment _environment;

    public ServerImageRepository(IWebHostEnvironment environment, ILogger<ImageRepository> logger)
        : base(logger)
    {
        _environment = environment;
    }

    protected override async Task<byte[]> LoadImageBytesAsync(string filename)
    {
        var clientWwwRootPath = Path.Combine(
            _environment.ContentRootPath,
            "..",
            "BlazorUnitedApp.Client",
            "wwwroot"
        );

        var filePath = Path.Combine(clientWwwRootPath, "sample-data", filename);
        return await File.ReadAllBytesAsync(filePath);
    }
}
