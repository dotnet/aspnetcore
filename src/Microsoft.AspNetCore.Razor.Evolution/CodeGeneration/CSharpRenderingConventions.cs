// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class CSharpRenderingConventions
    {
        public CSharpRenderingConventions(CSharpCodeWriter writer)
        {
            Writer = writer;
        }

        protected CSharpCodeWriter Writer { get; }

        public virtual string StartWriteMethod => "Write(" /* ORIGINAL: WriteMethodName */;

        public virtual string StartWriteLiteralMethod => "WriteLiteral(" /* ORIGINAL: WriteLiteralMethodName */;

        public virtual string StartBeginWriteAttributeMethod => "BeginWriteAttribute(" /* ORIGINAL: BeginWriteAttributeMethodName */;

        public virtual string StartWriteAttributeValueMethod => "WriteAttributeValue(" /* ORIGINAL: WriteAttributeValueMethodName */;

        public virtual string StartEndWriteAttributeMethod => "EndWriteAttribute(" /* ORIGINAL: EndWriteAttributeMethodName */;
    }
}
