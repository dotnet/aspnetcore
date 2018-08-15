// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    /* https://tools.ietf.org/html/rfc7540#section-6.3
        +-+-------------------------------------------------------------+
        |E|                  Stream Dependency (31)                     |
        +-+-------------+-----------------------------------------------+
        |   Weight (8)  |
        +-+-------------+
    */
    public partial class Http2Frame
    {
        private const int PriorityWeightOffset = 4;

        public int PriorityStreamDependency
        {
            get => (int)Bitshifter.ReadUInt31BigEndian(Payload);
            set => Bitshifter.WriteUInt31BigEndian(Payload, (uint)value);
        }

        public bool PriorityIsExclusive
        {
            get => (Payload[0] & 0x80) != 0;
            set
            {
                if (value)
                {
                    Payload[0] |= 0x80;
                }
                else
                {
                    Payload[0] &= 0x7f;
                }
            }
        }

        public byte PriorityWeight
        {
            get => Payload[PriorityWeightOffset];
            set => Payload[PriorityWeightOffset] = value;
        }


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
}
