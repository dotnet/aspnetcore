// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.JS;
using Microsoft.AspNetCore.Blazor.Internal.Common.FileProviders;
using Microsoft.AspNetCore.Blazor.Mono;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace Microsoft.AspNetCore.Blazor.Build.Core.FileSystem
{
    internal class FrameworkFileProvider : CompositeMountedFileProvider
    {
        public FrameworkFileProvider(string clientAssemblyPath)
            : base(GetContents(clientAssemblyPath))
        {
        }

        private static (string, IFileProvider)[] GetContents(string clientAssemblyPath)
        {
            return new[]
            {
                ("/", MonoStaticFileProvider.JsFiles),
                ("/", BlazorBrowserFileProvider.Instance),
                ("/_bin", CreateBinDirFileProvider(clientAssemblyPath))
            };
        }

        private static IFileProvider CreateBinDirFileProvider(string clientAssemblyPath)
            => new ReferencedAssemblyFileProvider(
                    Path.GetFileNameWithoutExtension(clientAssemblyPath),
                    new ReferencedAssemblyResolver(
                        MonoStaticFileProvider.BclFiles,
                        Path.GetDirectoryName(clientAssemblyPath)));
    }
}
