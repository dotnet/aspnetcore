// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class ExpressionChunk : Chunk
    {
        public string Code { get; set; }

        public override string ToString()
        {
            return Start + " = " + Code;
        }
    }
}
