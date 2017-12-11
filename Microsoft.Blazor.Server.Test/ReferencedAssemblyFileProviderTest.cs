// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Mono;
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
                typeof (HostedInAspNet.Client.Program).Assembly,
                MonoStaticFileProvider.Instance);
            Assert.Collection(provider.GetDirectoryContents("/"), item =>
            {
                Assert.Equal("/bin", item.PhysicalPath);
                Assert.True(item.IsDirectory);
            });
        }

        [Fact]
        public void FindsEntrypointAssemblyAndReferencedAssemblies()
        {
            var provider = new ReferencedAssemblyFileProvider(
                typeof(HostedInAspNet.Client.Program).Assembly,
                MonoStaticFileProvider.Instance);
            var contents = provider.GetDirectoryContents("/bin").OrderBy(i => i.Name).ToList();
            Assert.Collection(contents,
                item => { Assert.Equal("/bin/HostedInAspNet.Client.dll", item.PhysicalPath); },
                item => { Assert.Equal("/bin/mscorlib.dll", item.PhysicalPath); },
                item => { Assert.Equal("/bin/System.Console.dll", item.PhysicalPath); },
                item => { Assert.Equal("/bin/System.Core.dll", item.PhysicalPath); },
                item => { Assert.Equal("/bin/System.dll", item.PhysicalPath); },
                item => { Assert.Equal("/bin/System.Runtime.dll", item.PhysicalPath); });
        }
    }
}
