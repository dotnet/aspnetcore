// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Specifies the contracts for a <see cref="TextWriter"/> that buffers its content.
    /// </summary>
    [AssemblyNeutral]
    public interface IBufferedTextWriter
    {
        /// <summary>
        /// Gets a flag that determines if content is currently being buffered.
        /// </summary>
        bool IsBuffering { get; }

        /// <summary>
        /// Copies the buffered content to the <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The writer to copy the contents to./param>
        void CopyTo(TextWriter writer);

        /// <summary>
        /// Asynchronously copies the buffered content to the <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The writer to copy the contents to./param>
        /// <returns>A <see cref="Task"/> representing the copy operation.</returns>
        Task CopyToAsync(TextWriter writer);
    }
}