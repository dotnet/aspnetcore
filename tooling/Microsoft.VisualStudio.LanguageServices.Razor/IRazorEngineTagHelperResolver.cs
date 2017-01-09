// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal interface IRazorEngineTagHelperResolver
    {
        Task<IEnumerable<TagHelperDescriptor>> GetRazorEngineTagHelpersAsync(Workspace workspace, Project project);
    }
}
