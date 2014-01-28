// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class RazorCommentCodeGenerator : BlockCodeGenerator
    {
        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
#if NET45
            // Flush the buffered statement since we're interrupting it with a comment.
            if (!String.IsNullOrEmpty(context.CurrentBufferedStatement))
            {
                context.MarkEndOfGeneratedCode();
                context.BufferStatementFragment(context.BuildCodeString(cw => cw.WriteLineContinuation()));
            }
            context.FlushBufferedStatement();
#endif
        }
    }
}
