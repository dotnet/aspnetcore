// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Blazor.Mono;
using System.Collections.Generic;
using System.Net.Mime;

namespace Microsoft.AspNetCore.Builder
{
    public static class BlazorAppBuilderExtensions
    {
        public static void UseBlazor(this IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = "/_framework",
                FileProvider = new MonoStaticFileProvider(),
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
