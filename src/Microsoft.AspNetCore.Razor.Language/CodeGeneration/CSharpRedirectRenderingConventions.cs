// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class CSharpRedirectRenderingConventions : CSharpRenderingConventions
    {
        public CSharpRedirectRenderingConventions(string redirectWriter, CSharpCodeWriter writer) 
            : base(writer)
        {
            RedirectWriter = redirectWriter;
        }

        public string RedirectWriter { get; }

        public override string StartWriteMethod => "WriteTo(" + RedirectWriter + ", " /* ORIGINAL: WriteToMethodName */;

        public override string StartWriteLiteralMethod => "WriteLiteralTo(" + RedirectWriter + ", " /* ORIGINAL: WriteLiteralToMethodName */;

        public override string StartBeginWriteAttributeMethod => "BeginWriteAttributeTo(" + RedirectWriter + ", " /* ORIGINAL: BeginWriteAttributeToMethodName */;

        public override string StartWriteAttributeValueMethod => "WriteAttributeValueTo(" + RedirectWriter + ", " /* ORIGINAL: WriteAttributeValueToMethodName */;

        public override string StartEndWriteAttributeMethod => "EndWriteAttributeTo(" + RedirectWriter /* ORIGINAL: EndWriteAttributeToMethodName */;
    }
}
