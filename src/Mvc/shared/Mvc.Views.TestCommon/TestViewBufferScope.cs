// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers
{
    public class TestViewBufferScope : IViewBufferScope
    {
        public IList<ViewBufferValue[]> CreatedBuffers { get; } = new List<ViewBufferValue[]>();

        public IList<ViewBufferValue[]> ReturnedBuffers { get; } = new List<ViewBufferValue[]>();

        public ViewBufferValue[] GetPage(int size)
        {
            var buffer = new ViewBufferValue[size];
            CreatedBuffers.Add(buffer);
            return buffer;
        }

        public void ReturnSegment(ViewBufferValue[] segment)
        {
            ReturnedBuffers.Add(segment);
        }

        public TextWriter CreateWriter(TextWriter writer)
        {
            return new PagedBufferedTextWriter(ArrayPool<char>.Shared, writer);
        }
    }
}
