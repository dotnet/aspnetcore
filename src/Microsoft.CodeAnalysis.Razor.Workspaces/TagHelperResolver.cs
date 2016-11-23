// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.CodeAnalysis.Razor
{
    internal abstract class TagHelperResolver : ILanguageService
    {
        public abstract Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync(
            Project project,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
