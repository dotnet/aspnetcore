// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public static class RazorIRNodeSerializer
    {
        public static string Serialize(RazorIRNode node)
        {
            using (var writer = new StringWriter())
            {
                var walker = new Walker(writer);
                walker.Visit(node);

                return writer.ToString();
            }
        }

        private class Walker : RazorIRNodeWalker
        {
            private readonly RazorIRNodeWriter _visitor;
            private readonly TextWriter _writer;

            public Walker(TextWriter writer)
            {
                _visitor = new RazorIRNodeWriter(writer);
                _writer = writer;
            }

            public TextWriter Writer { get; }

            public override void VisitDefault(RazorIRNode node)
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
