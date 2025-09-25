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
#pragma warning disable ASPDEPR003 // Type or member is obsolete
            Options.Create(new MvcRazorRuntimeCompilationOptions()))
#pragma warning restore ASPDEPR003 // Type or member is obsolete
    {
        CompilationReferences = Array.Empty<MetadataReference>();
    }

    public override IReadOnlyList<MetadataReference> CompilationReferences { get; }
}
