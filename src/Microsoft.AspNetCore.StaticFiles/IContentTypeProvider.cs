// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.StaticFiles
{
    /// <summary>
    /// Used to look up MIME types given a file path
    /// </summary>
    public interface IContentTypeProvider
    {
        /// <summary>
        /// Given a file path, determine the MIME type
        /// </summary>
        /// <param name="subpath">A file path</param>
        /// <param name="contentType">The resulting MIME type</param>
        /// <returns>True if MIME type could be determined</returns>
        bool TryGetContentType(string subpath, out string contentType);
    }
}
