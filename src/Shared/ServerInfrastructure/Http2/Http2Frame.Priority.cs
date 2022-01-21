// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

/* https://tools.ietf.org/html/rfc7540#section-6.3
    +-+-------------------------------------------------------------+
    |E|                  Stream Dependency (31)                     |
    +-+-------------+-----------------------------------------------+
    |   Weight (8)  |
    +-+-------------+
*/
internal partial class Http2Frame
{
    public int PriorityStreamDependency { get; set; }

    public bool PriorityIsExclusive { get; set; }

    public byte PriorityWeight { get; set; }

    public void PreparePriority(int streamId, int streamDependency, bool exclusive, byte weight)
    {
        PayloadLength = 5;
        Type = Http2FrameType.PRIORITY;
        StreamId = streamId;
        PriorityStreamDependency = streamDependency;
        PriorityIsExclusive = exclusive;
        PriorityWeight = weight;
    }
}
