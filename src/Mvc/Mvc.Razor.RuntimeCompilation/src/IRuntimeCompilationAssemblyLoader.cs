// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    public interface IRuntimeCompilationAssemblyLoader
    {
        public Assembly Load(RazorCodeDocument codeDocument, MemoryStream assemblyStream, MemoryStream pdbStream);
    }
}
