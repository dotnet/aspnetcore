// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public abstract class CodeTargetBuilder
{
    public abstract RazorCodeDocument CodeDocument { get; }

    public abstract RazorCodeGenerationOptions Options { get; }

    public abstract ICollection<ICodeTargetExtension> TargetExtensions { get; }

    public abstract CodeTarget Build();
}
