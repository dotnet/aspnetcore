// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Ignitor
{
    public class CapturedRenderBatch
    {
        public CapturedRenderBatch(int id, byte[] data)
        {
            Id = id;
            Data = data;
        }

        public int Id { get; }
        public byte[] Data { get; }
    }
}
