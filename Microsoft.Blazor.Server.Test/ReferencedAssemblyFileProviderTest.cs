// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.Blazor.Server.Test
{
    public class ReferencedAssemblyFileProviderTest
    {
        [Fact]
        public void FindsEntrypointAssemblyAndReferencedAssemblies()
        {
            var provider = new ReferencedAssemblyFileProvider<HostedInAspNet.Client.Program>();
            var contents = provider.GetDirectoryContents(string.Empty).OrderBy(i => i.Name).ToList();
            Assert.Collection(contents,
                item => { Assert.Equal("HostedInAspNet.Client.dll", item.PhysicalPath); },
                item => { Assert.Equal("mscorlib.dll", item.PhysicalPath); },
                item => { Assert.Equal("System.Console.dll", item.PhysicalPath); },
                item => { Assert.Equal("System.Core.dll", item.PhysicalPath); },
                item => { Assert.Equal("System.dll", item.PhysicalPath); },
                item => { Assert.Equal("System.Runtime.dll", item.PhysicalPath); });
        }
    }
}
