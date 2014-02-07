// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FileSystems;

namespace Microsoft.AspNet.StaticFiles.DirectoryFormatters
{
    /// <summary>
    /// Generates the view for a directory
    /// </summary>
    public interface IDirectoryFormatter
    {
        /// <summary>
        /// Generates the view for a directory.
        /// Implementers should properly handle HEAD requests.
        /// Implementers should set all necessary response headers (e.g. Content-Type, Content-Length, etc.).
        /// </summary>
        Task GenerateContentAsync(HttpContext context, IEnumerable<IFileInfo> contents);
    }
}
