// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// Represents the data of a file selected from an <see cref="InputFile"/> component.
    /// </summary>
    public interface IBrowserFile
    {
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the last modified date.
        /// </summary>
        DateTime LastModified { get; }

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        long Size { get; }

        /// <summary>
        /// Gets the MIME type of the file.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Opens the stream for reading the uploaded file.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to signal the cancellation of streaming file data.</param>
        Stream OpenReadStream(CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts the current image file to a new one of the specified file type and maximum file dimensions.
        /// </summary>
        /// <remarks>
        /// The image will be scaled to fit the specified dimensions while preserving the original aspect ratio.
        /// </remarks>
        /// <param name="format">The new image format.</param>
        /// <param name="maxWith">The maximum image width.</param>
        /// <param name="maxHeight">The maximum image height</param>
        /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
        Task<IBrowserFile> ToImageFileAsync(string format, int maxWith, int maxHeight);
    }
}
