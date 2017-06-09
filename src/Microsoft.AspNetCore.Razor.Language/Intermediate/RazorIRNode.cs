// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public abstract class RazorIRNode
    {
        public abstract ItemCollection Annotations { get; }

        public abstract RazorIRNodeCollection Children { get; }

        public abstract SourceSpan? Source { get; set; }

        public abstract void Accept(RazorIRNodeVisitor visitor);
    }
}
