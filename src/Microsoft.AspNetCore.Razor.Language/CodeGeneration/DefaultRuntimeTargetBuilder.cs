// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class DefaultRuntimeTargetBuilder : IRuntimeTargetBuilder
    {
        public DefaultRuntimeTargetBuilder(RazorCodeDocument codeDocument, RazorParserOptions options)
        {
            CodeDocument = codeDocument;
            Options = options;

            TargetExtensions = new List<IRuntimeTargetExtension>();
        }

        public RazorCodeDocument CodeDocument { get; }

        public RazorParserOptions Options { get; }

        public ICollection<IRuntimeTargetExtension> TargetExtensions { get; }

        public RuntimeTarget Build()
        {
            return new DefaultRuntimeTarget(Options, TargetExtensions);
        }
    }
}
