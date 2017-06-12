// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(IRazorEngineDocumentGenerator))]
    internal class DefaultRazorEngineDocumentGenerator : IRazorEngineDocumentGenerator
    {
        public async Task<RazorEngineDocument> GenerateDocumentAsync(Workspace workspace, Project project, string filePath, string text, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var client = await RazorLanguageServiceClientFactory.CreateAsync(workspace, cancellationToken);

                using (var session = await client.CreateSessionAsync(project.Solution))
                {
                    var document = await session.InvokeAsync<RazorEngineDocument>("GenerateDocumentAsync", new object[] { project.Id.Id, "Foo", filePath, text }).ConfigureAwait(false);
                    return document;
                }
            }
            catch (Exception exception)
            {
                throw new RazorLanguageServiceException(
                    typeof(DefaultRazorEngineDocumentGenerator).FullName,
                    nameof(GenerateDocumentAsync),
                    exception);
            }
        }
    }
}
#endif