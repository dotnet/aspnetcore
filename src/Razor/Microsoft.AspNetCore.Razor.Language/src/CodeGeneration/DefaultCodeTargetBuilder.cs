// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class DefaultCodeTargetBuilder : CodeTargetBuilder
    {
        public DefaultCodeTargetBuilder(RazorCodeDocument codeDocument, RazorCodeGenerationOptions options)
        {
            CodeDocument = codeDocument;
            Options = options;

            TargetExtensions = new List<ICodeTargetExtension>();
        }

        public override RazorCodeDocument CodeDocument { get; }

        public override RazorCodeGenerationOptions Options { get; }

        public override ICollection<ICodeTargetExtension> TargetExtensions { get; }

        public override CodeTarget Build()
        {
            return new DefaultCodeTarget(Options, TargetExtensions);
        }
    }
}
