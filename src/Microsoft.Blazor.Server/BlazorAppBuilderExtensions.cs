// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Blazor.Server.ClientFilesystem;
using System.Collections.Generic;
using System.Net.Mime;

namespace Microsoft.AspNetCore.Builder
{
    public static class BlazorAppBuilderExtensions
    {
        public static void UseBlazor<TProgram>(this IApplicationBuilder applicationBuilder)
        {
            var clientAppAssembly = typeof(TProgram).Assembly;

            applicationBuilder.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = "/_framework",
                FileProvider = ClientFileProvider.Instantiate(clientAppAssembly),
                ContentTypeProvider = CreateContentTypeProvider(),
            });
        }

        private static IContentTypeProvider CreateContentTypeProvider()
        {
            return new FileExtensionContentTypeProvider(new Dictionary<string, string>
            {
                { ".dll", MediaTypeNames.Application.Octet },
                { ".js", "application/javascript" },
                { ".mem", MediaTypeNames.Application.Octet },
                { ".wasm", MediaTypeNames.Application.Octet },
            });
        }
    }
}
