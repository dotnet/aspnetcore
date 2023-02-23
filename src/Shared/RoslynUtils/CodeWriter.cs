// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;

internal sealed class CodeWriter : IndentedTextWriter
{
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
}
