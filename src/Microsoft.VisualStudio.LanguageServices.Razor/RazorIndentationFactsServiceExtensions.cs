// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Razor
{
    public static class RazorIndentationFactsServiceExtensions
    {
        public static int? GetDesiredIndentation(
            this RazorIndentationFactsService service,
            RazorSyntaxTree syntaxTree,
            ITextSnapshot syntaxTreeSnapshot,
            ITextSnapshotLine line,
            int indentSize,
            int tabSize)
        {
            // The tricky thing here is that line.Snapshot is very likely newer
            var previousLine = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
            var trackingPoint = line.Snapshot.CreateTrackingPoint(line.End, PointTrackingMode.Negative);
            var previousLineEnd = trackingPoint.GetPosition(syntaxTreeSnapshot);

            Func<int, string> getLineContentDelegate = (lineIndex) => line.Snapshot.GetLineFromLineNumber(lineIndex).GetText();

            return service.GetDesiredIndentation(syntaxTree, previousLineEnd, getLineContentDelegate, indentSize, tabSize);
        }
    }
}