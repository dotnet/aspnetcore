// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DirectiveTokenDescriptor
    {
        public DirectiveTokenKind Kind { get; set; }

        public bool Optional { get; set; }
    }
}
