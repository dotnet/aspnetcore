// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.Repositories;

/// <summary>
/// An XML repository backed by a file system.
/// </summary>
public class FileSystemXmlRepository : IDeletableXmlRepository
{
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a <see cref="FileSystemXmlRepository"/> with keys stored at the given directory.
    /// </summary>
    /// <param name="directory">The directory in which to persist key material.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public FileSystemXmlRepository(DirectoryInfo directory, ILoggerFactory loggerFactory)
    {
        Directory = directory ?? throw new ArgumentNullException(nameof(directory));

        _logger = loggerFactory.CreateLogger<FileSystemXmlRepository>();

        try
        {
            if (ContainerUtils.IsContainer && !ContainerUtils.IsVolumeMountedFolder(Directory))
            {
                // warn users that keys may be lost when running in docker without a volume mounted folder
                _logger.UsingEphemeralFileSystemLocationInContainer(Directory.FullName);
            }
        }
        catch (Exception ex)
        {
            // Treat exceptions as non-fatal when attempting to detect docker.
            // These might occur if fstab is an unrecognized format, or if there are other unusual
            // file IO errors.
            _logger.LogTrace(ex, "Failure occurred while attempting to detect docker.");
        }
    }

    /// <summary>
    /// The default key storage directory.
    /// On Windows, this currently corresponds to "Environment.SpecialFolder.LocalApplication/ASP.NET/DataProtection-Keys".
    /// On Linux and macOS, this currently corresponds to "$HOME/.aspnet/DataProtection-Keys".
    /// </summary>
    /// <remarks>
    /// This property can return null if no suitable default key storage directory can
    /// be found, such as the case when the user profile is unavailable.
    /// </remarks>
    public static DirectoryInfo? DefaultKeyStorageDirectory => DefaultKeyStorageDirectories.Instance.GetKeyStorageDirectory();

    /// <summary>
    /// The directory into which key material will be written.
    /// </summary>
    public DirectoryInfo Directory { get; }

    /// <inheritdoc/>
    public virtual IReadOnlyCollection<XElement> GetAllElements()
    {
        // forces complete enumeration
        return GetAllElementsCore().ToList().AsReadOnly();
    }

    private IEnumerable<XElement> GetAllElementsCore()
    {
        Directory.Create(); // won't throw if the directory already exists

        // Find all files matching the pattern "*.xml".
        // Note: Inability to read any file is considered a fatal error (since the file may contain
        // revocation information), and we'll fail the entire operation rather than return a partial
        // set of elements. If a file contains well-formed XML but its contents are meaningless, we
        // won't fail that operation here. The caller is responsible for failing as appropriate given
        // that scenario.
        foreach (var fileSystemInfo in EnumerateFileSystemInfos())
        {
            yield return ReadElementFromFile(fileSystemInfo.FullName);
        }
    }

    private IEnumerable<FileSystemInfo> EnumerateFileSystemInfos()
    {
        return Directory.EnumerateFileSystemInfos("*.xml", SearchOption.TopDirectoryOnly);
    }

    private static bool IsSafeFilename(string filename)
    {
        // Must be non-empty and contain only a-zA-Z0-9, hyphen, and underscore.
        return (!String.IsNullOrEmpty(filename) && filename.All(c =>
            c == '-'
            || c == '_'
            || ('0' <= c && c <= '9')
            || ('A' <= c && c <= 'Z')
            || ('a' <= c && c <= 'z')));
    }

    private XElement ReadElementFromFile(string fullPath)
    {
        _logger.ReadingDataFromFile(fullPath);

        using (var fileStream = File.OpenRead(fullPath))
        {
            return XElement.Load(fileStream);
        }
    }

    /// <inheritdoc/>
    public virtual void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullThrowHelper.ThrowIfNull(element);

        if (!IsSafeFilename(friendlyName))
        {
            var newFriendlyName = Guid.NewGuid().ToString();
            _logger.NameIsNotSafeFileName(friendlyName, newFriendlyName);
            friendlyName = newFriendlyName;
        }

        StoreElementCore(element, friendlyName);
    }

    private void StoreElementCore(XElement element, string filename)
    {
        // We're first going to write the file to a temporary location. This way, another consumer
        // won't try reading the file in the middle of us writing it. Additionally, if our process
        // crashes mid-write, we won't end up with a corrupt .xml file.

        Directory.Create(); // won't throw if the directory already exists

        var tempFilename = Path.Combine(Directory.FullName, Guid.NewGuid().ToString() + ".tmp");
        var finalFilename = Path.Combine(Directory.FullName, filename + ".xml");

        // Create a temp file with the correct Unix file mode before moving it to the expected finalFilename.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var tempTempFilename = Path.GetTempFileName();
            File.Move(tempTempFilename, tempFilename);
        }

        try
        {
            using (var tempFileStream = File.OpenWrite(tempFilename))
            {
                element.Save(tempFileStream);
            }

            // Once the file has been fully written, perform the rename.
            // Renames are atomic operations on the file systems we support.
            _logger.WritingDataToFile(finalFilename);

            try
            {
                // Prefer the atomic move operation to avoid multi-process startup issues
                File.Move(tempFilename, finalFilename);
            }
            catch (IOException)
            {
                // Use File.Copy because File.Move on NFS shares has issues in .NET Core 2.0
                // See https://github.com/dotnet/aspnetcore/issues/2941 for more context
                File.Copy(tempFilename, finalFilename);
            }
        }
        finally
        {
            File.Delete(tempFilename); // won't throw if the file doesn't exist
        }
    }

    /// <inheritdoc/>
    public virtual bool DeleteElements(Action<IReadOnlyCollection<IDeletableElement>> chooseElements)
    {
        ArgumentNullThrowHelper.ThrowIfNull(chooseElements);

        var deletableElements = new List<DeletableElement>();

        foreach (var fileSystemInfo in EnumerateFileSystemInfos())
        {
            var fullPath = fileSystemInfo.FullName;
            var element = ReadElementFromFile(fullPath);
            deletableElements.Add(new DeletableElement(fileSystemInfo, element));
        }

        chooseElements(deletableElements);

        var elementsToDelete = deletableElements
            .Where(e => e.DeletionOrder.HasValue)
            .OrderBy(e => e.DeletionOrder.GetValueOrDefault());

        foreach (var deletableElement in elementsToDelete)
        {
            var fileSystemInfo = deletableElement.FileSystemInfo;
            _logger.DeletingFile(fileSystemInfo.FullName);
            try
            {
                fileSystemInfo.Delete();
            }
            catch (Exception ex)
            {
                Debug.Assert(fileSystemInfo.Exists, "Having previously been deleted should not have caused an exception");
                _logger.FailedToDeleteFile(fileSystemInfo.FullName, ex);
                // Stop processing deletions to avoid deleting a revocation entry for a key that we failed to delete.
                return false;
            }
        }

        return true;
    }

    private sealed class DeletableElement : IDeletableElement
    {
        public DeletableElement(FileSystemInfo fileSystemInfo, XElement element)
        {
            FileSystemInfo = fileSystemInfo;
            Element = element;
        }

        /// <inheritdoc/>
        public XElement Element { get; }

        /// <summary>The FileSystemInfo from which <see cref="Element"/> was read.</summary>
        public FileSystemInfo FileSystemInfo { get; }

        /// <inheritdoc/>
        public int? DeletionOrder { get; set; }
    }
}
