// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.BuildTools.Core.FrameworkFiles;
using Microsoft.Blazor.Mono;
using Mono.Cecil;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Blazor.Server.Test
{
    public class ReferencedAssemblyFileProviderTest
    {
        [Fact]
        public void RootDirContainsOnlyBinDir()
        {
            var provider = new ReferencedAssemblyFileProvider(
                "mscorlib",
                new ReferencedAssemblyResolver(MonoStaticFileProvider.BclFiles, string.Empty));
            Assert.Collection(provider.GetDirectoryContents("/"), item =>
            {
                Assert.Equal("/_bin", item.PhysicalPath);
                Assert.True(item.IsDirectory);
            });
        }

        [Fact]
        public void FindsReferencedAssemblyGraphSimple()
        {
            var provider = new ReferencedAssemblyFileProvider(
                "System.Linq.Expressions",
                new ReferencedAssemblyResolver(MonoStaticFileProvider.BclFiles, string.Empty));
            var contents = provider.GetDirectoryContents("/_bin").OrderBy(i => i.Name).ToList();
            Assert.Collection(contents,
                item => { Assert.Equal("/_bin/mscorlib.dll", item.PhysicalPath); },
                item => { Assert.Equal("/_bin/System.Core.dll", item.PhysicalPath); },
                item => { Assert.Equal("/_bin/System.dll", item.PhysicalPath); },
                item => { Assert.Equal("/_bin/System.Linq.Expressions.dll", item.PhysicalPath); });
        }

        [Fact]
        public void FindsReferencedAssemblyGraphRealistic()
        {
            // Arrange
            var standaloneAppAssembly = typeof(StandaloneApp.Program).Assembly;
            var provider = new ReferencedAssemblyFileProvider(
                standaloneAppAssembly.GetName().Name,
                new ReferencedAssemblyResolver(
                    MonoStaticFileProvider.BclFiles,
                    Path.GetDirectoryName(standaloneAppAssembly.Location)));
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
                "/_bin/Microsoft.Blazor.dll",
                "/_bin/mscorlib.dll",
                "/_bin/netstandard.dll",
                "/_bin/StandaloneApp.dll",
                "/_bin/System.Console.dll",
                "/_bin/System.Core.dll",
                "/_bin/System.Diagnostics.StackTrace.dll",
                "/_bin/System.dll",
                "/_bin/System.Globalization.Extensions.dll",
                "/_bin/System.Runtime.dll",
                "/_bin/System.Runtime.InteropServices.RuntimeInformation.dll",
                "/_bin/System.Runtime.Serialization.Primitives.dll",
                "/_bin/System.Runtime.Serialization.Xml.dll",
                "/_bin/System.Security.Cryptography.Algorithms.dll",
                "/_bin/System.Security.SecureString.dll",
                "/_bin/System.Xml.XPath.XDocument.dll",
            };

            // Act
            var contents = provider.GetDirectoryContents("/_bin")
                .OrderBy(i => i.Name, StringComparer.InvariantCulture).ToList();

            // Assert
            Assert.Equal(expectedContents.Length, contents.Count);
            for (var i = 0; i < expectedContents.Length; i++)
            {
                Assert.Equal(expectedContents[i], contents[i].PhysicalPath);
            }
        }

        private static (AssemblyDefinition, byte[]) GetBclAssemblyForTest(string name)
        {
            var possibleFilenames = new[] { $"/bcl/{name}.dll", $"/bcl/Facades/{name}.dll" };
            var fileInfo = possibleFilenames
                .Select(MonoStaticFileProvider.BclFiles.GetFileInfo)
                .First(item => item.Exists);
            using (var data = new MemoryStream())
            {
                fileInfo.CreateReadStream().CopyTo(data);
                return (AssemblyDefinition.ReadAssembly(fileInfo.CreateReadStream()), data.ToArray());
            }
        }
    }
}
