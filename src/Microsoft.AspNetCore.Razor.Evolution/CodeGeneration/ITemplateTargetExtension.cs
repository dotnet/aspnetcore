// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal interface ITemplateTargetExtension : IRuntimeTargetExtension
    {
        void WriteTemplate(CSharpRenderingContext context, TemplateIRNode node);
    }
}
