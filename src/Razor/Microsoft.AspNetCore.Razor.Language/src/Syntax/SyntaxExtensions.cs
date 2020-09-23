// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal partial class MarkupTextLiteralSyntax
    {
        protected override string GetDebuggerDisplay()
        {
            return string.Format("{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
        }
    }

    internal partial class MarkupEphemeralTextLiteralSyntax
    {
        protected override string GetDebuggerDisplay()
        {
            return string.Format("{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
        }
    }

    internal partial class CSharpStatementLiteralSyntax
    {
        protected override string GetDebuggerDisplay()
        {
            return string.Format("{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
        }
    }

    internal partial class CSharpExpressionLiteralSyntax
    {
        protected override string GetDebuggerDisplay()
        {
            return string.Format("{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
        }
    }

    internal partial class CSharpEphemeralTextLiteralSyntax
    {
        protected override string GetDebuggerDisplay()
        {
            return string.Format("{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
        }
    }

    internal partial class UnclassifiedTextLiteralSyntax
    {
        protected override string GetDebuggerDisplay()
        {
            return string.Format("{0} [{1}]", base.GetDebuggerDisplay(), this.GetContent());
        }
    }
}
