// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal enum ParserState
    {
        CData,
        CodeTransition,
        DoubleTransition,
        EOF,
        MarkupComment,
        MarkupText,
        Misc,
        RazorComment,
        SpecialTag,
        Tag,
        Unknown,
        XmlPI,
    }
}
