// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class ClassifiedSpanWriter
{
    private readonly RazorSyntaxTree _syntaxTree;
    private readonly TextWriter _writer;

    public ClassifiedSpanWriter(TextWriter writer, RazorSyntaxTree syntaxTree)
    {
        _writer = writer;
        _syntaxTree = syntaxTree;
    }

    public virtual void Visit()
    {
        var classifiedSpans = _syntaxTree.GetClassifiedSpans();
        foreach (var span in classifiedSpans)
        {
            VisitClassifiedSpan(span);
            WriteNewLine();
        }
    }

    public virtual void VisitClassifiedSpan(ClassifiedSpanInternal span)
    {
        WriteClassifiedSpan(span);
    }

    protected void WriteClassifiedSpan(ClassifiedSpanInternal span)
    {
        Write($"{span.SpanKind} span at {span.Span} (Accepts:{span.AcceptedCharacters})");
        WriteSeparator();
        Write($"Parent: {span.BlockKind} block at {span.BlockSpan}");
    }

    protected void WriteSeparator()
    {
        Write(" - ");
    }

    protected void WriteNewLine()
    {
        _writer.WriteLine();
    }

    protected void Write(object value)
    {
        _writer.Write(value);
    }
}
