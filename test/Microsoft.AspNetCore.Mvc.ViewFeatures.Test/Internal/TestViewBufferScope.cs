// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class TestViewBufferScope : IViewBufferScope
    {
        public const int DefaultBufferSize = 128;
        private readonly int _bufferSize;

        public TestViewBufferScope(int bufferSize = DefaultBufferSize)
        {
            _bufferSize = bufferSize;
        }

        public IList<ViewBufferValue[]> ReturnedBuffers { get; } = new List<ViewBufferValue[]>();

        public ViewBufferValue[] GetSegment() => new ViewBufferValue[_bufferSize];

        public void ReturnSegment(ViewBufferValue[] segment)
        {
            ReturnedBuffers.Add(segment);
        }

        public ViewBufferTextWriter CreateWriter(TextWriter writer)
        {
            return new ViewBufferTextWriter(ArrayPool<char>.Shared, writer);
        }
    }
}
