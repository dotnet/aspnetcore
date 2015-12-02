// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Razor.Buffer
{
    public class TestRazorBufferScope : IRazorBufferScope
    {
        public const int BufferSize = 128;
        private readonly int _offset;
        private readonly int _count;

        public TestRazorBufferScope()
            : this(0, BufferSize)
        {

        }

        public TestRazorBufferScope(int offset, int count)
        {
            _offset = offset;
            _count = count;
        }

        public RazorBufferSegment GetSegment()
        {
            var razorValues = new RazorValue[BufferSize];
            return new RazorBufferSegment(new ArraySegment<RazorValue>(razorValues, _offset, _count));
        }
    }
}
