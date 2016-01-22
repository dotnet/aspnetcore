// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Represents a file sent with the HttpRequest.
    /// </summary>
    public interface IFormFile
    {
        /// <summary>
        /// Gets the raw Content-Type header of the uploaded file.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Gets the raw Content-Disposition header of the uploaded file.
        /// </summary>
        string ContentDisposition { get; }

        /// <summary>
        /// Gets the header dictionary of the uploaded file.
        /// </summary>
        IHeaderDictionary Headers { get; }

        /// <summary>
        /// Gets the file length in bytes.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Gets the name from the Content-Disposition header.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the file name from the Content-Disposition header.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Opens the request stream for reading the uploaded file.
        /// </summary>
        Stream OpenReadStream();

        /// <summary>
        /// Saves the contents of the uploaded file.
        /// </summary>
        /// <param name="path">The path of the file to create.</param>
        void SaveAs(string path);

        /// <summary>
        /// Asynchronously saves the contents of the uploaded file.
        /// </summary>
        /// <param name="path">The path of the file to create.</param>
        /// <param name="cancellationToken"></param>
        Task SaveAsAsync(string path, CancellationToken cancellationToken = default(CancellationToken));
    }
}
