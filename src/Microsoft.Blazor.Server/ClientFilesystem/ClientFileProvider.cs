// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Browser;
using Microsoft.Blazor.Mono;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Reflection;

namespace Microsoft.Blazor.Server.ClientFilesystem
{
    internal static class ClientFileProvider
    {
        public static IFileProvider Instantiate(string clientAssemblyPath)
            => new CompositeFileProvider(
                MonoStaticFileProvider.JsFiles,
                BlazorBrowserFileProvider.Instance,
                new ReferencedAssemblyFileProvider(
                    Path.GetFileNameWithoutExtension(clientAssemblyPath),
                    new ReferencedAssemblyResolver(
                        MonoStaticFileProvider.BclFiles,
                        Path.GetDirectoryName(clientAssemblyPath))));
    }
}
