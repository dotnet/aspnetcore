// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class CSharpRedirectRenderingConventions : CSharpRenderingConventions
    {
        private readonly string _redirectWriter;

        public CSharpRedirectRenderingConventions(string redirectWriter, CSharpCodeWriter writer) 
            : base(writer)
        {
            _redirectWriter = redirectWriter;
        }

        public override string StartWriteMethod => "WriteTo(" + _redirectWriter + ", " /* ORIGINAL: WriteToMethodName */;

        public override string StartWriteLiteralMethod => "WriteLiteralTo(" + _redirectWriter + ", " /* ORIGINAL: WriteLiteralToMethodName */;

        public override string StartBeginWriteAttributeMethod => "BeginWriteAttributeTo(" + _redirectWriter + ", " /* ORIGINAL: BeginWriteAttributeToMethodName */;

        public override string StartWriteAttributeValueMethod => "WriteAttributeValueTo(" + _redirectWriter + ", " /* ORIGINAL: WriteAttributeValueToMethodName */;

        public override string StartEndWriteAttributeMethod => "EndWriteAttributeTo(" + _redirectWriter /* ORIGINAL: EndWriteAttributeToMethodName */;
    }
}
