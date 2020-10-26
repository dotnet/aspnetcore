// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class RuntimeDependenciesResolverTest
    {
        [Fact]
        public void FindsReferenceAssemblyGraph_ForStandaloneApp()
        {
            // Arrange
            var standaloneAppAssembly = typeof(StandaloneApp.Program).Assembly;
            var mainAssemblyLocation = standaloneAppAssembly.Location;

            var hintPaths = ReadContent(standaloneAppAssembly, "StandaloneApp.referenceHints.txt");
            var bclLocations = ReadContent(standaloneAppAssembly, "StandaloneApp.bclLocations.txt");

            var expectedContents = new[]
            {
                /*
                 The current Mono WASM BCL forwards from netstandard.dll to various facade assemblies
                 in which small bits of implementation live, such as System.Xml.XPath.XDocument. So
                 if you reference netstandard, then you also reference System.Xml.XPath.XDocument.dll,
                 even though you're very unlikely to be calling it at runtime. That's why the following
                 list (for a very basic Blazor app) is longer than you'd expect.

                 These redundant references could be stripped out during publishing, but it's still
                 unfortunate that in development mode you'd see all these unexpected assemblies get
                 fetched from the server. We should try to get the Mono WASM BCL reorganized so that
                 all the implementation goes into mscorlib.dll, with the facade assemblies existing only
                 in case someone (or some 3rd party assembly) references them directly, but with their
                 implementations 100% forwarding to mscorlib.dll. Then in development you'd fetch far
                 fewer assemblies from the server, and during publishing, illink would remove all the
                 uncalled implementation code from mscorlib.dll anyway.
                 */
                "Microsoft.AspNetCore.Components.dll",
                "Microsoft.AspNetCore.Components.pdb",
                "Microsoft.AspNetCore.Components.Forms.dll",
                "Microsoft.AspNetCore.Components.Forms.pdb",
                "Microsoft.AspNetCore.Components.Web.dll",
                "Microsoft.AspNetCore.Components.Web.pdb",
                "Microsoft.AspNetCore.Components.WebAssembly.dll",
                "Microsoft.AspNetCore.Components.WebAssembly.pdb",
                "Microsoft.Bcl.AsyncInterfaces.dll",
                "Microsoft.Extensions.Configuration.Abstractions.dll",
                "Microsoft.Extensions.Configuration.dll",
                "Microsoft.Extensions.Configuration.FileExtensions.dll",
                "Microsoft.Extensions.Configuration.Json.dll",
                "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
                "Microsoft.Extensions.DependencyInjection.dll",
                "Microsoft.Extensions.FileProviders.Abstractions.dll",
                "Microsoft.Extensions.FileProviders.Physical.dll",
                "Microsoft.Extensions.FileSystemGlobbing.dll",
                "Microsoft.Extensions.Logging.dll",
                "Microsoft.Extensions.Logging.Abstractions.dll",
                "Microsoft.Extensions.Options.dll",
                "Microsoft.Extensions.Primitives.dll",
                "Microsoft.JSInterop.dll",
                "Microsoft.JSInterop.WebAssembly.dll",
                "Microsoft.JSInterop.WebAssembly.pdb",
                "Mono.Security.dll",
                "mscorlib.dll",
                "netstandard.dll",
                "StandaloneApp.dll",
                "StandaloneApp.pdb",
                "System.dll",
                "System.Buffers.dll",
                "System.ComponentModel.Annotations.dll",
                "System.ComponentModel.DataAnnotations.dll",
                "System.ComponentModel.Composition.dll",
                "System.Core.dll",
                "System.Data.dll",
                "System.Data.DataSetExtensions.dll",
                "System.Drawing.Common.dll",
                "System.IO.Compression.dll",
                "System.IO.Compression.FileSystem.dll",
                "System.Memory.dll",
                "System.Net.Http.dll",
                "System.Net.Http.Json.dll",
                "System.Net.Http.WebAssemblyHttpHandler.dll",
                "System.Numerics.dll",
                "System.Numerics.Vectors.dll",
                "System.Runtime.CompilerServices.Unsafe.dll",
                "System.Runtime.Serialization.dll",
                "System.ServiceModel.Internals.dll",
                "System.Text.Encodings.Web.dll",
                "System.Text.Json.dll",
                "System.Threading.Tasks.Extensions.dll",
                "System.Transactions.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
                "WebAssembly.Bindings.dll",
                "WebAssembly.Net.WebSockets.dll",
            }.OrderBy(i => i, StringComparer.Ordinal)
            .ToArray();

            // Act

            var paths = ResolveBlazorRuntimeDependencies
                .ResolveRuntimeDependenciesCore(
                   mainAssemblyLocation,
                   hintPaths,
                   bclLocations);

            var contents = paths
                .Select(p => Path.GetFileName(p))
                .OrderBy(i => i, StringComparer.Ordinal)
                .ToArray();

            var expected = new HashSet<string>(expectedContents);
            var actual = new HashSet<string>(contents);

            var contentNotFound = expected.Except(actual);
            var additionalContentFound = actual.Except(expected);

            // Assert
            if (contentNotFound.Any() || additionalContentFound.Any())
            {
                throw new ContentMisMatchException
                {
                    ContentNotFound = contentNotFound,
                    AdditionalContentFound = additionalContentFound,
                };
            }

            Assert.Equal(expectedContents, contents);
        }

        private string[] ReadContent(Assembly standaloneAppAssembly, string fileName)
        {
            using var resource = standaloneAppAssembly.GetManifestResourceStream(fileName);
            using var streamReader = new StreamReader(resource);

            return streamReader.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        }

        private class ContentMisMatchException : Xunit.Sdk.XunitException
        {
            public IEnumerable<string> ContentNotFound { get; set; }

            public IEnumerable<string> AdditionalContentFound { get; set; }

            public override string Message
            {
                get
                {
                    var error = new StringBuilder();
                    if (ContentNotFound.Any())
                    {
                        error.Append($"Expected content not found: ")
                            .AppendJoin(", ", ContentNotFound);
                    }

                    if (AdditionalContentFound.Any())
                    {
                        error.Append("Unexpected content found: ")
                            .AppendJoin(", ", AdditionalContentFound);
                    }

                    return error.ToString();
                }
            }
        }
    }
}
