// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Chunks
{
    public class LiteralChunk : Chunk
    {
        public string Text { get; set; }

        public override string ToString()
        {
            return Start + " = " + Text;
        }
    }
}
