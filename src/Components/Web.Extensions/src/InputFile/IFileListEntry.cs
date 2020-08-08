// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// Represents the data of a file selected from an <see cref="InputFile"/> component.
    /// </summary>
    public interface IFileListEntry
    {
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Gets the last modified date.
        /// </summary>
        DateTime? LastModified { get; }

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Gets the MIME type of the file.
        /// </summary>
        string? Type { get; }

        /// <summary>
        /// Gets the file's path relative to the base directory selected in a directory picker.
        /// This is not supported on all browsers: see
        /// https://developer.mozilla.org/en-US/docs/Web/API/File/webkitRelativePath.
        /// </summary>
        string? RelativePath { get; }

        /// <summary>
        /// Gets a stream of the file's data.
        /// </summary>
        Stream Data { get; }

        /// <summary>
        /// Called when a chunk of data is read from the file stream.
        /// </summary>
        event EventHandler OnDataRead;

        /// <summary>
        /// Converts the current image file to a new one of the specified file type and maximum file dimensions.
        /// </summary>
        /// <remarks>
        /// The image will be scaled to fit the specified dimensions while preserving the original aspect ratio.
        /// </remarks>
        /// <param name="format">The new image format.</param>
        /// <param name="maxWith">The maximum image width.</param>
        /// <param name="maxHeight">The maximum image height</param>
        /// <returns></returns>
        Task<IFileListEntry> ToImageFileAsync(string format, int maxWith, int maxHeight);
    }
}
