// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Browser;
using Microsoft.Blazor.Mono;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace Microsoft.Blazor.BuildTools.Core.FrameworkFiles
{
    internal static class FrameworkFileProvider
    {
        public static IFileProvider Instantiate(string clientAssemblyPath)
            => new CompositeFileProvider(
                MonoStaticFileProvider.JsFiles,          // /_framework/wasm/*, /framework/asmjs/*
                BlazorBrowserFileProvider.Instance,      // /_framework/blazor.js
                BinDirFileProvider(clientAssemblyPath)); // /_framework/_bin/*

        private static IFileProvider BinDirFileProvider(string clientAssemblyPath)
            => new ReferencedAssemblyFileProvider(
                    Path.GetFileNameWithoutExtension(clientAssemblyPath),
                    new ReferencedAssemblyResolver(
                        MonoStaticFileProvider.BclFiles,
                        Path.GetDirectoryName(clientAssemblyPath)));
    }
}
