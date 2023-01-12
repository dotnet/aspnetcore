// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure;

internal class CodeWriter
{
    private readonly StringBuilder _codeBuilder = new();
    private int _indent;

    public CodeWriter(StringBuilder stringBuilder)
    {
        _codeBuilder = stringBuilder;
    }

    public void StartBlock()
    {
        WriteLine("{");
        Indent();
    }

    public void EndBlock()
    {
        Unindent();
        WriteLine("}");
    }

    public void Indent()
    {
        _indent++;
    }

    public void Unindent()
    {
        _indent--;
    }

    public void Indent(int tabs)
    {
        _indent += tabs;
    }

    public void Unindent(int tabs)
    {
        _indent -= tabs;
    }

    public void WriteLineNoIndent(string value)
    {
        _codeBuilder.AppendLine(value);
    }

    public void WriteNoIndent(string value)
    {
        _codeBuilder.Append(value);
    }

    public void Write(string value)
    {
        if (_indent > 0)
        {
            _codeBuilder.Append(new string(' ', _indent * 4));
        }
        _codeBuilder.Append(value);
    }

    public void WriteLine(string value)
    {
        if (_indent > 0)
        {
            _codeBuilder.Append(new string(' ', _indent * 4));
        }
        _codeBuilder.AppendLine(value);
    }

    public override string ToString()
    {
        return _codeBuilder.ToString();
    }
}
