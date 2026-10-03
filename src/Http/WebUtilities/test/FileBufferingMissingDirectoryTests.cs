// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.WebUtilities;

public class FileBufferingMissingDirectoryTests
{
    [Fact]
    public async Task FileBufferingReadStream_RecreatesDeletedTempDirectory_WhenDirectoryIsString()
    {
        var tempDirectory = CreateDeletedDirectoryPath();
        try
        {
            using var stream = new FileBufferingReadStream(new MemoryStream(new byte[100]), 1, null, tempDirectory);

            var buffer = new byte[100];
            var read = await stream.ReadAsync(buffer, 0, buffer.Length);

            Assert.Equal(100, read);
            Assert.True(Directory.Exists(tempDirectory));
            Assert.Single(Directory.GetFiles(tempDirectory, "ASPNETCORE_*.tmp"));
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectory);
        }
    }

    [Fact]
    public async Task FileBufferingReadStream_RecreatesDeletedTempDirectory_WhenDirectoryIsFactory()
    {
        var tempDirectory = CreateDeletedDirectoryPath();
        try
        {
            using var stream = new FileBufferingReadStream(new MemoryStream(new byte[100]), 1, null, () => tempDirectory);

            var buffer = new byte[100];
            var read = await stream.ReadAsync(buffer, 0, buffer.Length);

            Assert.Equal(100, read);
            Assert.True(Directory.Exists(tempDirectory));
            Assert.Single(Directory.GetFiles(tempDirectory, "ASPNETCORE_*.tmp"));
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectory);
        }
    }

    [Fact]
    public async Task FileBufferingWriteStream_RecreatesDeletedTempDirectory()
    {
        var tempDirectory = CreateDeletedDirectoryPath();
        try
        {
            await using var stream = new FileBufferingWriteStream(1, null, () => tempDirectory);

            await stream.WriteAsync(new byte[100], 0, 100);

            Assert.True(Directory.Exists(tempDirectory));
            Assert.Single(Directory.GetFiles(tempDirectory, "ASPNETCORE_*.tmp"));
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectory);
        }
    }

    // Creates a directory and deletes it again, simulating a cleanup tool or operator
    // removing the temp directory after its path was already resolved.
    private static string CreateDeletedDirectoryPath()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        Directory.Delete(path);
        return path;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
