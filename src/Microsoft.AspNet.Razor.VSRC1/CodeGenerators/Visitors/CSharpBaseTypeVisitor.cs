// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Chunks;

namespace Microsoft.AspNet.Razor.CodeGenerators.Visitors
{
    public class CSharpBaseTypeVisitor : CodeVisitor<CSharpCodeWriter>
    {
        public CSharpBaseTypeVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
            : base(writer, context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
        }

        public string CurrentBaseType { get; set; }

        protected override void Visit(SetBaseTypeChunk chunk)
        {
            CurrentBaseType = chunk.TypeName;
        }
    }
}
