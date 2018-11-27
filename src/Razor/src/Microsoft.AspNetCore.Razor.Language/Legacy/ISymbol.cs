// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal interface ISymbol
    {
        Span Parent { get; set; }

        string Content { get; }

        SourceLocation Start { get; }
    }
}
