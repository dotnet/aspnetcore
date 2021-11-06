// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class EditResult
{
    public EditResult(PartialParseResultInternal result, SyntaxNode editedNode)
    {
        Result = result;
        EditedNode = editedNode;
    }

    public PartialParseResultInternal Result { get; set; }
    public SyntaxNode EditedNode { get; set; }
}
