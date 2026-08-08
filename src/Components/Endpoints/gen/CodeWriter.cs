// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

// A minimal indenting writer. The generated file is read whenever someone is diagnosing why a
// component was or was not described, so it is written to be legible rather than compact.
internal sealed class CodeWriter
{
    private readonly StringBuilder _builder = new();
    private int _indent;

    public void WriteLine()
        => _builder.AppendLine();

    public void WriteLine(string text)
    {
        if (_indent > 0)
        {
            _builder.Append(' ', _indent * 4);
        }

        _builder.AppendLine(text);
    }

    public void OpenBrace()
    {
        WriteLine("{");
        _indent++;
    }

    public void OpenBracket()
    {
        WriteLine("[");
        _indent++;
    }

    public void CloseBrace() => Close("}");

    public void CloseBraceWithComma() => Close("},");

    public void CloseBraceWithSemicolon() => Close("};");

    public void CloseBracketWithSemicolon() => Close("];");

    private void Close(string text)
    {
        _indent--;
        WriteLine(text);
    }

    public override string ToString() => _builder.ToString();
}
