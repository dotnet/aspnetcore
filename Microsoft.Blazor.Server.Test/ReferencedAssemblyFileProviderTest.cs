// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Mono;
using Microsoft.Blazor.Server.ClientFilesystem;
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
            var (entrypoint, entrypointData) = GetBclAssemblyForTest("mscorlib");
            var provider = new ReferencedAssemblyFileProvider(
                entrypoint,
                entrypointData,
                MonoStaticFileProvider.BclFiles);
            Assert.Collection(provider.GetDirectoryContents("/"), item =>
            {
                Assert.Equal("/bin", item.PhysicalPath);
                Assert.True(item.IsDirectory);
            });
        }

        [Fact]
        public void FindsReferencedAssemblyGraphSimple()
        {
            var (entrypoint, entrypointData) = GetBclAssemblyForTest("System.Linq.Expressions");
            var provider = new ReferencedAssemblyFileProvider(
                entrypoint,
                entrypointData,
                MonoStaticFileProvider.BclFiles);
            var contents = provider.GetDirectoryContents("/bin").OrderBy(i => i.Name).ToList();
            Assert.Collection(contents,
                item => { Assert.Equal("/bin/mscorlib.dll", item.PhysicalPath); },
                item => { Assert.Equal("/bin/System.Core.dll", item.PhysicalPath); },
                item => { Assert.Equal("/bin/System.dll", item.PhysicalPath); },
                item => { Assert.Equal("/bin/System.Linq.Expressions.dll", item.PhysicalPath); });
        }

        [Fact]
        public void FindsReferencedAssemblyGraphRealistic()
        {
            // Arrange
            var standaloneAppAssemblyLocation = typeof(StandaloneApp.Program).Assembly.Location;
            var provider = new ReferencedAssemblyFileProvider(
                AssemblyDefinition.ReadAssembly(standaloneAppAssemblyLocation),
                File.ReadAllBytes(standaloneAppAssemblyLocation),
                MonoStaticFileProvider.BclFiles);
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
                "/bin/Microsoft.Blazor.dll",
                "/bin/mscorlib.dll",
                "/bin/netstandard.dll",
                "/bin/StandaloneApp.dll",
                "/bin/System.Console.dll",
                "/bin/System.Core.dll",
                "/bin/System.Diagnostics.StackTrace.dll",
                "/bin/System.dll",
                "/bin/System.Globalization.Extensions.dll",
                "/bin/System.Runtime.dll",
                "/bin/System.Runtime.InteropServices.RuntimeInformation.dll",
                "/bin/System.Runtime.Serialization.Primitives.dll",
                "/bin/System.Runtime.Serialization.Xml.dll",
                "/bin/System.Security.Cryptography.Algorithms.dll",
                "/bin/System.Security.SecureString.dll",
                "/bin/System.Xml.XPath.XDocument.dll",
            };

            // Act
            var contents = provider.GetDirectoryContents("/bin")
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
