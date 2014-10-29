// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// An <see cref="IFileSystem"/> that caches the results of <see cref="IFileSystem.GetFileInfo(string)"/> for a
    /// duration specified by <see cref="RazorViewEngineOptions.ExpirationBeforeCheckingFilesOnDisk"/>.
    /// </summary>
    public interface IRazorFileSystemCache : IFileSystem
    {
    }
}