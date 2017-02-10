// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class CSharpLiteralCodeConventions : CSharpRenderingConventions
    {
        public CSharpLiteralCodeConventions(CSharpCodeWriter writer) : base(writer)
        {
        }

        public override string StartWriteMethod => StartWriteLiteralMethod;
    }
}
