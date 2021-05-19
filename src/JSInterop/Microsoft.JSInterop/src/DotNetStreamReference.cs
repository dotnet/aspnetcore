// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DotNetStreamReference : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="leaveOpen"></param>
        public DotNetStreamReference(Stream stream, bool leaveOpen = false)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            LeaveOpen = leaveOpen;
        }

        internal Stream Stream { get; }

        internal bool LeaveOpen { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!LeaveOpen)
            {
                Stream.Dispose();
            }
        }
    }
}
