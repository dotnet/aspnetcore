// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

internal class TestRazorReferenceManager : RazorReferenceManager
{
    public TestRazorReferenceManager()
        : base(
            new ApplicationPartManager(),
            Options.Create(new MvcRazorRuntimeCompilationOptions()))
    {
        CompilationReferences = Array.Empty<MetadataReference>();
    }

    public override IReadOnlyList<MetadataReference> CompilationReferences { get; }
}
