// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpTypeMemberVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private CSharpCodeVisitor _csharpCodeVisitor;

        public CSharpTypeMemberVisitor(CSharpCodeWriter writer, CodeBuilderContext context)
            : base(writer, context)
        {
            _csharpCodeVisitor = new CSharpCodeVisitor(writer, context);
        }

        protected override void Visit(TypeMemberChunk chunk)
        {
            if (!String.IsNullOrEmpty(chunk.Code))
            {
                _csharpCodeVisitor.CreateCodeMapping(String.Empty, chunk.Code, chunk);
            }
        }
    }
}
