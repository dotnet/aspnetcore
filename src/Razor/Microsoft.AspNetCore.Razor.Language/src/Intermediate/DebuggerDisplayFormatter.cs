// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal class DebuggerDisplayFormatter : IntermediateNodeFormatterBase
{
    public DebuggerDisplayFormatter()
    {
        Writer = new StringWriter();
        ContentMode = FormatterContentMode.PreferContent;
    }

    public override string ToString()
    {
        return Writer.ToString();
    }
}
