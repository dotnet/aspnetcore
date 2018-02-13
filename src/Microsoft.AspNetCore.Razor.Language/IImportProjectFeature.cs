// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public interface IImportProjectFeature : IRazorProjectEngineFeature
    {
        IReadOnlyList<RazorSourceDocument> GetImports(RazorProjectItem projectItem);
    }
}
