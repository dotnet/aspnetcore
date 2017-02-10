// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class DefaultRuntimeTargetBuilder : IRuntimeTargetBuilder
    {
        public DefaultRuntimeTargetBuilder(RazorCodeDocument codeDocument, RazorParserOptions options)
        {
            CodeDocument = codeDocument;
            Options = options;
        }

        public RazorCodeDocument CodeDocument { get; }

        public RazorParserOptions Options { get; }

        public RuntimeTarget Build()
        {
            return new DefaultRuntimeTarget(Options);
        }
    }
}
