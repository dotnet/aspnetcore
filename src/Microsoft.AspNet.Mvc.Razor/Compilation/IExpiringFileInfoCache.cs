// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Provides cached access to file infos.
    /// </summary>
    public interface IFileInfoCache
    {
        /// <summary>
        /// Returns a cached <see cref="IFileInfo" /> for a given path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <returns>The cached <see cref="IFileInfo"/>.</returns>
        IFileInfo GetFileInfo(string virtualPath);
    }
}