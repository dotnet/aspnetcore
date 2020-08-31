// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.ProtectedBrowserStorage
{
    /// <summary>
    /// Contains the result of a protected browser storage operation.
    /// </summary>
    public readonly struct ProtectedBrowserStorageResult<T>
    {
        /// <summary>
        /// Gets whether the operation succeeded.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the result value of the operation.
        /// </summary>
        [MaybeNull]
        [AllowNull]
        public T Value { get; }

        internal ProtectedBrowserStorageResult(bool success, [AllowNull] T value)
        {
            Success = success;
            Value = value;
        }
    }
}
