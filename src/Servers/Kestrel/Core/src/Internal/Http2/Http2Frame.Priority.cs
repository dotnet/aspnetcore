// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Frame
    {
        public int PriorityStreamDependency
        {
            get => ((_data[PayloadOffset] << 24)
                | (_data[PayloadOffset + 1] << 16)
                | (_data[PayloadOffset + 2] << 8)
                | _data[PayloadOffset + 3]) & 0x7fffffff;
            set
            {
                _data[PayloadOffset] = (byte)((value & 0x7f000000) >> 24);
                _data[PayloadOffset + 1] = (byte)((value & 0x00ff0000) >> 16);
                _data[PayloadOffset + 2] = (byte)((value & 0x0000ff00) >> 8);
                _data[PayloadOffset + 3] = (byte)(value & 0x000000ff);
            }
        }


        public bool PriorityIsExclusive
        {
            get => (_data[PayloadOffset] & 0x80000000) != 0;
            set
            {
                if (value)
                {
                    _data[PayloadOffset] |= 0x80;
                }
                else
                {
                    _data[PayloadOffset] &= 0x7f;
                }
            }
        }

        public byte PriorityWeight
        {
            get => _data[PayloadOffset + 4];
            set => _data[PayloadOffset] = value;
        }


        public void PreparePriority(int streamId, int streamDependency, bool exclusive, byte weight)
        {
            Length = 5;
            Type = Http2FrameType.PRIORITY;
            StreamId = streamId;
            PriorityStreamDependency = streamDependency;
            PriorityIsExclusive = exclusive;
            PriorityWeight = weight;
        }
    }
}
