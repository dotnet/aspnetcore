// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to the Web Locks API from a <see cref="Navigator"/>.
/// </summary>
public static class NavigatorLockManagerExtensions
{
    extension(Navigator navigator)
    {
        /// <summary>
        /// Gets the lock manager of the origin.
        /// </summary>
        public LockManager Locks
            => navigator.GetOrAddFeature(static context => new LockManager(context.JSRuntime, context.CallbackFactory));
    }
}
