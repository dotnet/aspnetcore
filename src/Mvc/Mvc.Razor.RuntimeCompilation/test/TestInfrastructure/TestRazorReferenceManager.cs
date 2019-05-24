// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
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
}
