// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeTree
    {
        public CodeTree()
        {
            Chunks = new List<Chunk>();
        }

        public IList<Chunk> Chunks { get; set; }
    }
}
