// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal enum BlockKindInternal
{
    // We want this to match the values in BlockKind so that the values are maintained when casting.

    // Code
    Statement = 0,
    Directive = 1,
    Expression = 3,

    // Markup
    Markup = 5,
    Template = 7,

    // Special
    Comment = 8,
    Tag = 9,
    HtmlComment = 10
}
