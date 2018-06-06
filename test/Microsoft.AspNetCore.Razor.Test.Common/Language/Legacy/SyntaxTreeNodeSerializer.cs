// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public static class SyntaxTreeNodeSerializer
    {
        internal static string Serialize(SyntaxTreeNode node)
        {
            using (var writer = new StringWriter())
            {
                var walker = new Walker(writer);
                walker.Visit(node);

                return writer.ToString();
            }
        }

        private class Walker : SyntaxTreeNodeWalker
        {
            private readonly SyntaxTreeNodeWriter _visitor;
            private readonly TextWriter _writer;

            public Walker(TextWriter writer)
            {
                _visitor = new SyntaxTreeNodeWriter(writer);
                _writer = writer;
            }

            public TextWriter Writer { get; }

            public override void Visit(SyntaxTreeNode node)
            {
                _visitor.Visit(node);
                _writer.WriteLine();

                if (node is Block block)
                {
                    _visitor.Depth++;
                    base.VisitDefault(block);
                    _visitor.Depth--;
                }
            }
        }
    }
}
