// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Provides version hash for a specified file.
    /// </summary>
    public interface IFileVersionProvider
    {
        /// <summary>
        /// Adds version query parameter to the specified file path.
        /// </summary>
        /// <param name="requestPathBase">The base path for the current HTTP request.</param>
        /// <param name="path">The path of the file to which version should be added.</param>
        /// <returns>Path containing the version query string.</returns>
        string AddFileVersionToPath(PathString requestPathBase, string path);
    }
}
