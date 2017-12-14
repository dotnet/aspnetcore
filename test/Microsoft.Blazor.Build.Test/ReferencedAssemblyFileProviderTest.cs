// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Build.Core.FileSystem;
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
        public void RootDirContainsOnlyDlls()
        {
            var provider = new ReferencedAssemblyFileProvider(
                "mscorlib",
                new ReferencedAssemblyResolver(MonoStaticFileProvider.BclFiles, string.Empty));
            foreach (var item in provider.GetDirectoryContents("/"))
            {
                Assert.False(item.IsDirectory);
                Assert.EndsWith(".dll", item.Name);
            }
        }

        [Fact]
        public void FindsReferencedAssemblyGraphSimple()
        {
            var provider = new ReferencedAssemblyFileProvider(
                "System.Linq.Expressions",
                new ReferencedAssemblyResolver(MonoStaticFileProvider.BclFiles, string.Empty));
            var contents = provider.GetDirectoryContents("").OrderBy(i => i.Name).ToList();
            Assert.Collection(contents,
                item => { Assert.Equal("/mscorlib.dll", item.PhysicalPath); },
                item => { Assert.Equal("/System.Core.dll", item.PhysicalPath); },
                item => { Assert.Equal("/System.dll", item.PhysicalPath); },
                item => { Assert.Equal("/System.Linq.Expressions.dll", item.PhysicalPath); });
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
                "/Microsoft.Blazor.dll",
                "/mscorlib.dll",
                "/netstandard.dll",
                "/StandaloneApp.dll",
                "/System.Console.dll",
                "/System.Core.dll",
                "/System.Diagnostics.StackTrace.dll",
                "/System.dll",
                "/System.Globalization.Extensions.dll",
                "/System.Runtime.dll",
                "/System.Runtime.InteropServices.RuntimeInformation.dll",
                "/System.Runtime.Serialization.Primitives.dll",
                "/System.Runtime.Serialization.Xml.dll",
                "/System.Security.Cryptography.Algorithms.dll",
                "/System.Security.SecureString.dll",
                "/System.Xml.XPath.XDocument.dll",
            };

            // Act
            var contents = provider.GetDirectoryContents("")
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
