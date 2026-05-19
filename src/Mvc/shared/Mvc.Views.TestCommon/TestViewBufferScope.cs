// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

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
