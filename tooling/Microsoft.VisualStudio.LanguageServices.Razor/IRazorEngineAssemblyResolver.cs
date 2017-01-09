// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal interface IRazorEngineAssemblyResolver
    {
        Task<IEnumerable<RazorEngineAssembly>> GetRazorEngineAssembliesAsync(Project project);
    }
}
