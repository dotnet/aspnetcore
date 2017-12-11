// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.Blazor.Mono.Test
{
    public class MonoStaticFileProviderTest
    {
        [Fact]
        public void SuppliesJsFiles()
        {
            // The collection is small enough that we can assert the exact full list

            Assert.Collection(MonoStaticFileProvider.JsFiles.GetDirectoryContents("/").OrderBy(i => i.Name),
                item => Assert.Equal("/asmjs", item.PhysicalPath),
                item => Assert.Equal("/wasm", item.PhysicalPath));

            Assert.Collection(MonoStaticFileProvider.JsFiles.GetDirectoryContents("/asmjs").OrderBy(i => i.Name),
                item => Assert.Equal("/asmjs/mono.asm.js", item.PhysicalPath),
                item => Assert.Equal("/asmjs/mono.js", item.PhysicalPath),
                item => Assert.Equal("/asmjs/mono.js.mem", item.PhysicalPath));

            Assert.Collection(MonoStaticFileProvider.JsFiles.GetDirectoryContents("/wasm").OrderBy(i => i.Name),
                item => Assert.Equal("/wasm/mono.js", item.PhysicalPath),
                item => Assert.Equal("/wasm/mono.wasm", item.PhysicalPath));
        }

        [Fact]
        public void SuppliesBclFiles()
        {
            Assert.Collection(MonoStaticFileProvider.BclFiles.GetDirectoryContents("/"),
                item => Assert.Equal("/bcl", item.PhysicalPath));

            Assert.Collection(MonoStaticFileProvider.BclFiles.GetDirectoryContents("/bcl").OrderBy(i => i.Name),
                item => Assert.Equal("/bcl/Facades", item.PhysicalPath),
                item => Assert.Equal("/bcl/mscorlib.dll", item.PhysicalPath),
                item => Assert.Equal("/bcl/System.Core.dll", item.PhysicalPath),
                item => Assert.Equal("/bcl/System.dll", item.PhysicalPath));

            // Not an exhaustive list. The full list is long.
            var actualFacades = MonoStaticFileProvider.BclFiles.GetDirectoryContents("/bcl/Facades");
            Assert.Contains(actualFacades, item => item.PhysicalPath == "/bcl/Facades/netstandard.dll");
            Assert.Contains(actualFacades, item => item.PhysicalPath == "/bcl/Facades/System.Console.dll");
        }
    }
}
