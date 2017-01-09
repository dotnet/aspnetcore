// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Microsoft.CodeAnalysis;
using System.Composition;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(IRazorEngineTagHelperResolver))]
    internal class DefaultRazorEngineTagHelperResolver : IRazorEngineTagHelperResolver
    {
        public async Task<IEnumerable<TagHelperDescriptor>> GetRazorEngineTagHelpersAsync(Workspace workspace, Project project)
        {
            var client = await RazorLanguageServiceClientFactory.CreateAsync(workspace, CancellationToken.None);

            using (var session = await client.CreateSessionAsync(project.Solution))
            {
                var results = await session.InvokeAsync<IEnumerable<TagHelperDescriptor>>("GetTagHelpersAsync", new object[] { project.Id.Id, "Foo" }).ConfigureAwait(false);
                return results;
            }
        }
    }
}
