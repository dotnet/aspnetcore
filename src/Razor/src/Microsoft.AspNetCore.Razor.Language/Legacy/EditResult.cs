// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
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
}
