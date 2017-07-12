// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(IRazorEngineDirectiveResolver))]
    internal class DefaultRazorEngineDirectiveResolver : IRazorEngineDirectiveResolver
    {
        public async Task<IEnumerable<DirectiveDescriptor>> GetRazorEngineDirectivesAsync(Workspace workspace, Project project, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var client = await RazorLanguageServiceClientFactory.CreateAsync(workspace, cancellationToken);

                using (var session = await client.CreateSessionAsync(project.Solution))
                {
                    var directives = await session.InvokeAsync<IEnumerable<DirectiveDescriptor>>(
                            "GetDirectivesAsync", 
                            new object[] { project.Id.Id, "Foo", },
                            cancellationToken)
                        .ConfigureAwait(false);
                    return directives;
                }
            }
            catch (Exception exception)
            {
                throw new RazorLanguageServiceException(
                    typeof(DefaultRazorEngineDirectiveResolver).FullName,
                    nameof(GetRazorEngineDirectivesAsync),
                    exception);
            }
        }
    }
}
#endif
