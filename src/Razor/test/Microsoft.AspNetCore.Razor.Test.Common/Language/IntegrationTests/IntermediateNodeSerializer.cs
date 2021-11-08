// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

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
