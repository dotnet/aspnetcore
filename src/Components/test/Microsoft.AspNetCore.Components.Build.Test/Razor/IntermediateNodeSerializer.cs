// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public static class IntermediateNodeSerializer
    {
        public static string Serialize(IntermediateNode node)
        {
            using (var writer = new StringWriter())
            {
                var walker = new Walker(writer);
                walker.Visit(node);

                return writer.ToString();
            }
        }

        private class Walker : IntermediateNodeWalker
        {
            private readonly IntermediateNodeWriter _visitor;
            private readonly TextWriter _writer;

            public Walker(TextWriter writer)
            {
                _visitor = new IntermediateNodeWriter(writer);
                _writer = writer;
            }

            public TextWriter Writer { get; }

            public override void VisitDefault(IntermediateNode node)
            {
                _visitor.Visit(node);
                _writer.WriteLine();

                _visitor.Depth++;
                base.VisitDefault(node);
                _visitor.Depth--;
            }
        }
    }
}