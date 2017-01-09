// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.CodeAnalysis.Remote.Razor
{
    internal class RazorLanguageService : ServiceHubServiceBase
    {
        public RazorLanguageService(Stream stream, IServiceProvider serviceProvider) 
            : base(stream, serviceProvider)
        {
        }

        public async Task<IEnumerable<TagHelperDescriptor>> GetTagHelpersAsync(byte[] projectIdBytes, string projectDebugName, byte[] solutionChecksum, CancellationToken cancellationToken = default(CancellationToken))
        {
            var projectId = ProjectId.CreateFromSerialized(new Guid(projectIdBytes), projectDebugName);

            var solution = await GetSolutionAsync().ConfigureAwait(false);
            var project = solution.GetProject(projectId);

            var resolver = new DefaultTagHelperResolver();
            var results = await resolver.GetTagHelpersAsync(project, cancellationToken).ConfigureAwait(false);

            return results;
        }

        public Task<IEnumerable<DirectiveDescriptor>> GetDirectivesAsync(byte[] projectIdBytes, string projectDebugName, byte[] solutionChecksum, CancellationToken cancellationToken = default(CancellationToken))
        {
            var engine = RazorEngine.Create();
            var directives = engine.Features.OfType<IRazorDirectiveFeature>().FirstOrDefault()?.Directives;
            return Task.FromResult(directives ?? Enumerable.Empty<DirectiveDescriptor>());
        }

        public Task<GeneratedDocument> GenerateDocumentAsync(byte[] projectIdBytes, string projectDebugName, string filename, string text, byte[] solutionChecksum, CancellationToken cancellationToken = default(CancellationToken))
        {
            var engine = RazorEngine.Create();

            RazorSourceDocument source;
            using (var stream = new MemoryStream())
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);

                stream.Seek(0L, SeekOrigin.Begin);
                source = RazorSourceDocument.ReadFrom(stream, filename, Encoding.UTF8);
            }

            var code = RazorCodeDocument.Create(source);
            engine.Process(code);

            var csharp = code.GetCSharpDocument();
            if (csharp == null)
            {
                throw new InvalidOperationException();
            }

            return Task.FromResult(new GeneratedDocument() { Text = csharp.GeneratedCode, });
        }
    }
}
