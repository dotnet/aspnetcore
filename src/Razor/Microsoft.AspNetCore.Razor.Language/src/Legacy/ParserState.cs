// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

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
