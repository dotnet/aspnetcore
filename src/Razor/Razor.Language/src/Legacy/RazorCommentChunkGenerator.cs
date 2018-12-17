// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class RazorCommentChunkGenerator : ParentChunkGenerator
    {
        public override void Accept(ParserVisitor visitor, Block block)
        {
            visitor.VisitCommentBlock(this, block);
        }
    }
}
