// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class TestDefaultExtensionAssemblyLoader : DefaultExtensionAssemblyLoader
    {
        public TestDefaultExtensionAssemblyLoader(string baseDirectory)
            : base(baseDirectory)
        {
        }

        protected override Assembly LoadFromPathUnsafeCore(string filePath)
        {
            // Force a load from streams so we don't lock the files on disk. This way we can test
            // shadow copying without leaving a mess behind.
            var bytes = File.ReadAllBytes(filePath);
            var stream = new MemoryStream(bytes);
            return LoadContext.LoadFromStream(stream);
        }
    }
}
