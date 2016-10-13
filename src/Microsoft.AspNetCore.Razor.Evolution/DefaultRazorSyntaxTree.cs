// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorSyntaxTree : RazorSyntaxTree
    {
        public DefaultRazorSyntaxTree(Block root, IReadOnlyList<RazorError> diagnostics)
        {
            Root = root;
            Diagnostics = diagnostics;
        }

        internal override IReadOnlyList<RazorError> Diagnostics { get; }

        internal override Block Root { get; }
    }
}
