// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage
{
    /// <summary>
    /// Contains the result of a protected browser storage operation.
    /// </summary>
    public readonly struct ProtectedBrowserStorageResult<TValue>
    {
        /// <summary>
        /// Gets whether the operation succeeded.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the result value of the operation.
        /// </summary>
        public TValue? Value { get; }

        internal ProtectedBrowserStorageResult(bool success, TValue? value)
        {
            Success = success;
            Value = value;
        }
    }
}
