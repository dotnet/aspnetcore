// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to the Storage API from a <see cref="Navigator"/>.
/// </summary>
public static class NavigatorStorageManagerExtensions
{
    extension(Navigator navigator)
    {
        /// <summary>
        /// Gets the storage manager of the current origin.
        /// </summary>
        public StorageManager Storage
            => navigator.GetOrAddFeature(static context => new StorageManager(context.JSRuntime));
    }
}
