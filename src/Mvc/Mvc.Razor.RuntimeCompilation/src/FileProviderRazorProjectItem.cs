// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

/// <summary>
/// A file provider <see cref="RazorProjectItem"/>.
/// </summary>
public class FileProviderRazorProjectItem : RazorProjectItem
{
    private readonly string _root;
    private string? _relativePhysicalPath;
    private bool _isRelativePhysicalPathSet;

    /// <summary>
    /// Intializes a new instance of a <see cref="FileProviderRazorProjectItem"/>.
    /// </summary>
    /// <param name="fileInfo">The file info.</param>
    /// <param name="basePath">The base path.</param>
    /// <param name="filePath">The file path.</param>
    /// <param name="root">The root.</param>
    public FileProviderRazorProjectItem(IFileInfo fileInfo, string basePath, string filePath, string root) : this(fileInfo, basePath, filePath, root, fileKind: null)
    {
    }

    /// <summary>
    /// Intializes a new instance of a <see cref="FileProviderRazorProjectItem"/>.
    /// </summary>
    /// <param name="fileInfo">The file info.</param>
    /// <param name="basePath">The base path.</param>
    /// <param name="filePath">The file path.</param>
    /// <param name="root">The root.</param>
    /// <param name="fileKind">The kind of file.</param>
    public FileProviderRazorProjectItem(IFileInfo fileInfo, string basePath, string filePath, string root, string? fileKind)
    {
        FileInfo = fileInfo;
        BasePath = basePath;
        FilePath = filePath;
        FileKind = fileKind ?? FileKinds.GetFileKindFromFilePath(filePath);
        _root = root;
    }

    /// <summary>
    /// The <see cref="IFileInfo"/>.
    /// </summary>
    public IFileInfo FileInfo { get; }

    /// <inheritdoc/>
    public override string BasePath { get; }

    /// <inheritdoc/>
    public override string FilePath { get; }

    /// <inheritdoc/>
    public override string FileKind { get; }

    /// <inheritdoc/>
    public override bool Exists => FileInfo.Exists;

    /// <inheritdoc/>
    public override string PhysicalPath => FileInfo.PhysicalPath ?? string.Empty;

    /// <inheritdoc/>
    public override string? RelativePhysicalPath
    {
        get
        {
            if (!_isRelativePhysicalPathSet)
            {
                _isRelativePhysicalPathSet = true;

                if (Exists)
                {
                    if (_root != null &&
                        !string.IsNullOrEmpty(PhysicalPath) &&
                        PhysicalPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase) &&
                        PhysicalPath.Length > _root.Length &&
                        (PhysicalPath[_root.Length] == Path.DirectorySeparatorChar || PhysicalPath[_root.Length] == Path.AltDirectorySeparatorChar))
                    {
                        _relativePhysicalPath = PhysicalPath.Substring(_root.Length + 1); // Include leading separator
                    }
                }
            }

            return _relativePhysicalPath;
        }
    }

    /// <inheritdoc/>
    public override Stream Read()
    {
        return FileInfo.CreateReadStream();
    }
}
