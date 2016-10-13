// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal enum BlockType
    {
        // Code
        Statement,
        Directive,
        Functions,
        Expression,
        Helper,

        // Markup
        Markup,
        Section,
        Template,

        // Special
        Comment,
        Tag
    }
}
