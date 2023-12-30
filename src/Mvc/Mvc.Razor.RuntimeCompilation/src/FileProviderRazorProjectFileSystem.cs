// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

internal sealed class FileProviderRazorProjectFileSystem : RazorProjectFileSystem
{
    private const string RazorFileExtension = ".cshtml";
    private readonly RuntimeCompilationFileProvider _fileProvider;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public FileProviderRazorProjectFileSystem(RuntimeCompilationFileProvider fileProvider, IWebHostEnvironment hostingEnvironment)
    {
        ArgumentNullException.ThrowIfNull(fileProvider);
        ArgumentNullException.ThrowIfNull(hostingEnvironment);

        _fileProvider = fileProvider;
        _hostingEnvironment = hostingEnvironment;
    }

    public IFileProvider FileProvider => _fileProvider.FileProvider;

    public override RazorProjectItem GetItem(string path)
    {
        return GetItem(path, fileKind: null);
    }

    public override RazorProjectItem GetItem(string path, string? fileKind)
    {
        path = NormalizeAndEnsureValidPath(path);
        var fileInfo = FileProvider.GetFileInfo(path);

        return new FileProviderRazorProjectItem(fileInfo, basePath: string.Empty, filePath: path, root: _hostingEnvironment.ContentRootPath, fileKind);
    }

    public override IEnumerable<RazorProjectItem> EnumerateItems(string path)
    {
        path = NormalizeAndEnsureValidPath(path);
        return EnumerateFiles(FileProvider.GetDirectoryContents(path), path, prefix: string.Empty);
    }

    private IEnumerable<RazorProjectItem> EnumerateFiles(IDirectoryContents directory, string basePath, string prefix)
    {
        if (directory.Exists)
        {
            foreach (var fileInfo in directory)
            {
                if (fileInfo.IsDirectory)
                {
                    var relativePath = prefix + "/" + fileInfo.Name;
                    var subDirectory = FileProvider.GetDirectoryContents(JoinPath(basePath, relativePath));
                    var children = EnumerateFiles(subDirectory, basePath, relativePath);
                    foreach (var child in children)
                    {
                        yield return child;
                    }
                }
                else if (string.Equals(RazorFileExtension, Path.GetExtension(fileInfo.Name), StringComparison.OrdinalIgnoreCase))
                {
                    var filePath = prefix + "/" + fileInfo.Name;

                    yield return new FileProviderRazorProjectItem(fileInfo, basePath, filePath: filePath, root: _hostingEnvironment.ContentRootPath);
                }
            }
        }
    }

    private static string JoinPath(string path1, string path2)
    {
        var hasTrailingSlash = path1.EndsWith('/');
        var hasLeadingSlash = path2.StartsWith('/');
        if (hasLeadingSlash && hasTrailingSlash)
        {
            return string.Concat(path1, path2.AsSpan(1));
        }
        else if (hasLeadingSlash || hasTrailingSlash)
        {
            return path1 + path2;
        }

        return path1 + "/" + path2;
    }
}
