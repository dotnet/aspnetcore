// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Blazor.Mono.Test
{
    public class MonoStaticFileProviderTest
    {
        [Fact]
        public void SuppliesMonoFiles()
        {
            // This is not an exhaustive list. The set of BCL facade types is long and
            // will probably change. This test is just to verify the resource embedding
            // and filename mapping is working correctly.
            var expectedFiles = new[]
            {
                "/asmjs/mono.asm.js",
                "/asmjs/mono.js.mem",
                "/wasm/mono.wasm",
                "/bcl/mscorlib.dll",
                "/bcl/Facades/System.Collections.dll",
            };

            foreach (var name in expectedFiles)
            {
                var fileInfo = MonoStaticFileProvider.Instance.GetFileInfo(name);
                Assert.True(fileInfo.Exists);
                Assert.False(fileInfo.IsDirectory);
                Assert.True(fileInfo.Length > 0);
            }
        }

        [Fact]
        public void DoesNotSupplyUnexpectedFiles()
        {
            var notExpectedFiles = new[]
            {
                "",
                "mono",
                "wasm",
                "/wasm",
                "/wasm/",
                "wasm/mono.wasm",
                "/wasm/../wasm/mono.wasm",
            };

            foreach (var name in notExpectedFiles)
            {
                var fileInfo = MonoStaticFileProvider.Instance.GetFileInfo(name);
                Assert.False(fileInfo.Exists);
            }
        }
    }
}
