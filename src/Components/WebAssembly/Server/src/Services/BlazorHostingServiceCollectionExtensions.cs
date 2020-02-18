// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BlazorHostingServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorStaticFilesConfiguration(this IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<StaticFileOptions>, ClientSideBlazorStaticFilesConfiguration>());
            return services;
        }

        private class ClientSideBlazorStaticFilesConfiguration : IConfigureOptions<StaticFileOptions>
        {
            private readonly IWebHostEnvironment _webHostEnvironment;

            public ClientSideBlazorStaticFilesConfiguration(IWebHostEnvironment webHostEnvironment)
            {
                _webHostEnvironment = webHostEnvironment;
            }

            public void Configure(StaticFileOptions options)
            {
                options.FileProvider = _webHostEnvironment.WebRootFileProvider;
                var contentTypeProvider = new FileExtensionContentTypeProvider();
                AddMapping(contentTypeProvider, ".dll", MediaTypeNames.Application.Octet);
                // We unconditionally map dlls as there will be no dlls in the output folder for
                // release builds unless BlazorEnableDebugging is explicitly set to true.
                AddMapping(contentTypeProvider, ".pdb", MediaTypeNames.Application.Octet);

                options.ContentTypeProvider = contentTypeProvider;
            }

            private static void AddMapping(FileExtensionContentTypeProvider provider, string name, string mimeType)
            {
                if (!provider.Mappings.ContainsKey(name))
                {
                    provider.Mappings.Add(name, mimeType);
                }
            }
        }
    }
}
