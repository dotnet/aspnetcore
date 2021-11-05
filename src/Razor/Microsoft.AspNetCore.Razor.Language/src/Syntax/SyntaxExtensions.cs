// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal partial class MarkupTextLiteralSyntax
{
    protected override string GetDebuggerDisplay()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
    }
}

internal partial class MarkupEphemeralTextLiteralSyntax
{
    protected override string GetDebuggerDisplay()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
    }
}

internal partial class CSharpStatementLiteralSyntax
{
    protected override string GetDebuggerDisplay()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
    }
}

internal partial class CSharpExpressionLiteralSyntax
{
    protected override string GetDebuggerDisplay()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
    }
}

internal partial class CSharpEphemeralTextLiteralSyntax
{
    protected override string GetDebuggerDisplay()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
    }
}

internal partial class UnclassifiedTextLiteralSyntax
{
    protected override string GetDebuggerDisplay()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
    }
}
