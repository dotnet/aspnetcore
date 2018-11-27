// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class ClassDeclarationIntermediateNode : MemberDeclarationIntermediateNode
    {
        public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

        public IList<string> Modifiers { get; } = new List<string>();

        public string ClassName { get; set; }

        public string BaseType { get; set; }

        public IList<string> Interfaces { get; set; } = new List<string>();

        public IList<TypeParameter> TypeParameters { get; set; } = new List<TypeParameter>();

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitClassDeclaration(this);
        }
    }
}
