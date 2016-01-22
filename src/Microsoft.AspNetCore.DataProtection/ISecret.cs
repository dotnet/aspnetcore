// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Represents a secret value.
    /// </summary>
    public interface ISecret : IDisposable
    {
        /// <summary>
        /// The length (in bytes) of the secret value.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Writes the secret value to the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer which should receive the secret value.</param>
        /// <remarks>
        /// The buffer size must exactly match the length of the secret value.
        /// </remarks>
        void WriteSecretIntoBuffer(ArraySegment<byte> buffer);
    }
}
