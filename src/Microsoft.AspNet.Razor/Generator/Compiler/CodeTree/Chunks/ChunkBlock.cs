// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class ChunkBlock : Chunk
    {
        public ChunkBlock()
        {
            Children = new List<Chunk>();
        }

        public IList<Chunk> Children { get; set; }
    }
}
