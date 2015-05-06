// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpBaseTypeVisitor : CodeVisitor<CSharpCodeWriter>
    {
        public CSharpBaseTypeVisitor([NotNull] CSharpCodeWriter writer, [NotNull] CodeBuilderContext context)
            : base(writer, context)
        {
        }

        public string CurrentBaseType { get; set; }

        protected override void Visit(SetBaseTypeChunk chunk)
        {
            CurrentBaseType = chunk.TypeName;
        }
    }
}
