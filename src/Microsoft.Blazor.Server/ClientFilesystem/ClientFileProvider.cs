// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Browser;
using Microsoft.Blazor.Mono;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace Microsoft.Blazor.Server.ClientFilesystem
{
    internal static class ClientFileProvider
    {
        public static IFileProvider Instantiate(Assembly clientApp)
            => new CompositeFileProvider(
                MonoStaticFileProvider.JsFiles,
                MonoStaticFileProvider.BclFiles, // TODO: Stop serving these, and serve the ReferencedAssemblyFileProvider instead
                BlazorBrowserFileProvider.Instance,
                new ReferencedAssemblyFileProvider(clientApp, MonoStaticFileProvider.BclFiles));
    }
}
