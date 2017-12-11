// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Blazor.Mono.Test
{
    public class MonoStaticFileProviderTest
    {
        [Fact]
        public void SuppliesJsFiles()
        {
            // The collection is small enough that we can assert the exact full list

            Assert.Collection(MonoStaticFileProvider.JsFiles.GetDirectoryContents("/"),
                item => Assert.Equal("/asmjs", item.PhysicalPath),
                item => Assert.Equal("/wasm", item.PhysicalPath));

            Assert.Collection(MonoStaticFileProvider.JsFiles.GetDirectoryContents("/asmjs"),
                item => Assert.Equal("/asmjs/mono.asm.js", item.PhysicalPath),
                item => Assert.Equal("/asmjs/mono.js", item.PhysicalPath),
                item => Assert.Equal("/asmjs/mono.js.mem", item.PhysicalPath));

            Assert.Collection(MonoStaticFileProvider.JsFiles.GetDirectoryContents("/wasm"),
                item => Assert.Equal("/wasm/mono.js", item.PhysicalPath),
                item => Assert.Equal("/wasm/mono.wasm", item.PhysicalPath));
        }

        [Fact]
        public void SuppliesBclFiles()
        {
            Assert.Collection(MonoStaticFileProvider.BclFiles.GetDirectoryContents("/"),
                item => Assert.Equal("/bin", item.PhysicalPath));

            Assert.Collection(MonoStaticFileProvider.BclFiles.GetDirectoryContents("/bin"),
                item => Assert.Equal("/bin/mscorlib.dll", item.PhysicalPath),
                item => Assert.Equal("/bin/System.Core.dll", item.PhysicalPath),
                item => Assert.Equal("/bin/System.dll", item.PhysicalPath),
                item => Assert.Equal("/bin/Facades", item.PhysicalPath));

            // Not an exhaustive list. The full list is long.
            var actualFacades = MonoStaticFileProvider.BclFiles.GetDirectoryContents("/bin/Facades");
            Assert.Contains(actualFacades, item => item.PhysicalPath == "/bin/Facades/netstandard.dll");
            Assert.Contains(actualFacades, item => item.PhysicalPath == "/bin/Facades/System.Console.dll");
        }
    }
}
