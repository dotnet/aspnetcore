// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BlazorHostingServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorStaticFilesConfiguration(this IServiceCollection services)
        {
            services.Configure<StaticFileOptions>(options =>
            {
            });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<StaticFileOptions>, ClientSideBlazorStaticFilesConfiguration>());

            return services;

        }

        private class ClientSideBlazorStaticFilesConfiguration : IConfigureOptions<StaticFileOptions>
        {
            public ClientSideBlazorStaticFilesConfiguration(IWebHostEnvironment webHostEnvironment)
            {
                WebHostEnvironment = webHostEnvironment;
            }

            public IWebHostEnvironment WebHostEnvironment { get; }

            public void Configure(StaticFileOptions options)
            {
                options.FileProvider = WebHostEnvironment.WebRootFileProvider;
                var contentTypeProvider = new FileExtensionContentTypeProvider();
                AddMapping(contentTypeProvider, ".dll", MediaTypeNames.Application.Octet);
                // For right now unconditionally enable debugging
                AddMapping(contentTypeProvider, ".pdb", MediaTypeNames.Application.Octet);
                options.ContentTypeProvider = contentTypeProvider;
                options.OnPrepareResponse = CacheHeaderSettings.SetCacheHeaders;
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
