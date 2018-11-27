// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    // Internal until we flesh out public RazorSyntaxTree API
    internal interface IRazorSyntaxTreePass : IRazorEngineFeature
    {
        int Order { get; }

        RazorSyntaxTree Execute(RazorCodeDocument codeDocument, RazorSyntaxTree syntaxTree);
    }
}