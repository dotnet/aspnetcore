// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public interface IInjectDirectiveTargetExtension : IRuntimeTargetExtension
    {
        void WriteInjectProperty(CSharpRenderingContext context, InjectDirectiveIRNode node);
    }
}
