// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.LanguageServices.Razor;

namespace Microsoft.CodeAnalysis.Remote.Razor
{
    internal class RazorLanguageService : ServiceHubServiceBase
    {
        [Obsolete("This will be removed as part of #976. ServiceHub still calls this constructor. Remove once ServiceHub can use the other one.")]
        public RazorLanguageService(Stream stream, IServiceProvider serviceProvider)
            : base(stream, serviceProvider)
        {
            Rpc.JsonSerializer.Converters.Add(new RazorDiagnosticJsonConverter());
        }

        public RazorLanguageService(IServiceProvider serviceProvider, Stream stream)
            : base(serviceProvider, stream)
        {
            Rpc.JsonSerializer.Converters.Add(new RazorDiagnosticJsonConverter());
        }

        public async Task<TagHelperResolutionResult> GetTagHelpersAsync(Guid projectIdBytes, string projectDebugName, IEnumerable<string> assemblyNameFilters, CancellationToken cancellationToken = default(CancellationToken))
        {
            var projectId = ProjectId.CreateFromSerialized(projectIdBytes, projectDebugName);

            var solution = await GetSolutionAsync().ConfigureAwait(false);
            var project = solution.GetProject(projectId);

            var resolver = new DefaultTagHelperResolver(designTime: true);
            var result = await resolver.GetTagHelpersAsync(project, assemblyNameFilters, cancellationToken).ConfigureAwait(false);

            return result;
        }

        public Task<IEnumerable<DirectiveDescriptor>> GetDirectivesAsync(Guid projectIdBytes, string projectDebugName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var projectId = ProjectId.CreateFromSerialized(projectIdBytes, projectDebugName);

            var engine = RazorEngine.Create();
            var directives = engine.Features.OfType<IRazorDirectiveFeature>().FirstOrDefault()?.Directives;
            return Task.FromResult(directives ?? Enumerable.Empty<DirectiveDescriptor>());
        }

        public Task<GeneratedDocument> GenerateDocumentAsync(Guid projectIdBytes, string projectDebugName, string filename, string text, CancellationToken cancellationToken = default(CancellationToken))
        {
            var projectId = ProjectId.CreateFromSerialized(projectIdBytes, projectDebugName);

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
