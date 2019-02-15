// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers
{
    internal class ViewBufferPage
    {
        public ViewBufferPage(ViewBufferValue[] buffer)
        {
            Buffer = buffer;
        }

        public ViewBufferValue[] Buffer { get; }

        public int Capacity => Buffer.Length;

        public int Count { get; set; }

        public bool IsFull => Count == Capacity;

        // Very common trivial method; nudge it to inline https://github.com/aspnet/Mvc/pull/8339
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(ViewBufferValue value) => Buffer[Count++] = value;
    }
}
