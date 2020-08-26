// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// Represents the data of a file selected from an <see cref="InputFile"/> component.
    /// <para>
    /// Note: Metadata is provided by the client and is untrusted.
    /// </para>
    /// </summary>
    public interface IBrowserFile
    {
        /// <summary>
        /// Gets the name of the file as specified by the browser.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the last modified date as specified by the browser.
        /// </summary>
        DateTimeOffset LastModified { get; }

        /// <summary>
        /// Gets the size of the file in bytes as specified by the browser.
        /// </summary>
        long Size { get; }

        /// <summary>
        /// Gets the MIME type of the file as specified by the browser.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Opens the stream for reading the uploaded file.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to signal the cancellation of streaming file data.</param>
        Stream OpenReadStream(CancellationToken cancellationToken = default);
    }
}
