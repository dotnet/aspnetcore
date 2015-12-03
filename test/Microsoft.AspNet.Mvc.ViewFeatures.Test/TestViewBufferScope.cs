// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ViewFeatures.Buffer
{
    public class TestViewBufferScope : IViewBufferScope
    {
        public const int DefaultBufferSize = 128;
        private readonly int _bufferSize;

        public TestViewBufferScope(int bufferSize = DefaultBufferSize)
        {
            _bufferSize = bufferSize;
        }

        public ViewBufferValue[] GetSegment() => new ViewBufferValue[_bufferSize];
    }
}
