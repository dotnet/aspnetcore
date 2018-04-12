// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor
{
    internal class RazorLanguageService : RazorServiceBase
    {
        public RazorLanguageService(Stream stream, IServiceProvider serviceProvider)
            : base(stream, serviceProvider)
        {
        }

        public async Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshotHandle projectHandle, string factoryTypeName, CancellationToken cancellationToken = default)
        {
            var project = await GetProjectSnapshotAsync(projectHandle, cancellationToken).ConfigureAwait(false);

            return await RazorServices.TagHelperResolver.GetTagHelpersAsync(project, factoryTypeName, cancellationToken);
        }
    }
}
