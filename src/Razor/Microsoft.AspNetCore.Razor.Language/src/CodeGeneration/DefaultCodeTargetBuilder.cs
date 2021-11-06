// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

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
