// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.DataProtection.Repositories
{
    /// <summary>
    /// This interface enables overridding the default storage location of keys on disk
    /// </summary>
    internal interface IDefaultKeyStorageDirectories
    {
        DirectoryInfo GetKeyStorageDirectory();

        DirectoryInfo GetKeyStorageDirectoryForAzureWebSites();
    }
}
