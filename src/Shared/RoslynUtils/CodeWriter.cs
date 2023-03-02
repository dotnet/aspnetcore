// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;

internal sealed class CodeWriter : IndentedTextWriter
{
    private bool _indentationInitialized;
    public CodeWriter(StringWriter stringWriter, int baseIndent) : base(stringWriter)
    {
        Indent = baseIndent;
    }

    public void StartBlock()
    {
        this.WriteLine("{");
        this.Indent++;
    }

    public void EndBlock()
    {
        this.Indent--;
        this.WriteLine("}");
    }

    // The IndentedTextWriter adds the indentation
    // _after_ writing the first line of text. This
    // method can be used ot initialize indentation
    // when an emit method might only emit one line
    // of code.
    public void InitializeIndent()
    {
        if (_indentationInitialized)
        {
            return;
        }

        for (var i = 0; i < Indent; i++)
        {
            Write(DefaultTabString);
        }
        _indentationInitialized = true;
    }
}
