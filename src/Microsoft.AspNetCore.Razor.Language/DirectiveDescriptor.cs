// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DirectiveDescriptor
    {
        public string Name { get; set; }

        public DirectiveDescriptorKind Kind { get; set; }

        public IReadOnlyList<DirectiveTokenDescriptor> Tokens { get; set; }
    }
}
