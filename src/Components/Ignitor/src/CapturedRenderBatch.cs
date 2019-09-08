// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
