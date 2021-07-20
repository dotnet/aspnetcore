// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Hosting.StaticWebAssets
{
    /// <summary>
    /// Loader for static web assets
    /// </summary>
    public class StaticWebAssetsLoader
    {
        internal const string StaticWebAssetsManifestName = "Microsoft.AspNetCore.StaticWebAssets.xml";

        /// <summary>
        /// Configure the <see cref="IWebHostEnvironment"/> to use static web assets.
        /// </summary>
        /// <param name="environment">The application <see cref="IWebHostEnvironment"/>.</param>
        /// <param name="configuration">The host <see cref="IConfiguration"/>.</param>
        public static void UseStaticWebAssets(IWebHostEnvironment environment, IConfiguration configuration)
        {
            var (manifest, isJson) = ResolveManifest(environment, configuration);
            using (manifest)
            {
                if (manifest != null)
                {
                    UseStaticWebAssetsCore(environment, manifest, isJson);
                }
            }
        }

        internal static void UseStaticWebAssetsCore(IWebHostEnvironment environment, Stream manifest, bool isJson)
        {
            if (isJson)
            {
                var staticWebAssetManifest = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.Parse(manifest);
                var provider = new ManifestStaticWebAssetFileProvider(
                    staticWebAssetManifest,
                    (contentRoot) => new PhysicalFileProvider(contentRoot));

                environment.WebRootFileProvider = new CompositeFileProvider(new[] { environment.WebRootFileProvider, provider });
                return;
            }

            var webRootFileProvider = environment.WebRootFileProvider;

            var additionalFiles = StaticWebAssetsReader.Parse(manifest)
                .Select(cr => new StaticWebAssetsFileProvider(cr.BasePath, cr.Path))
                .OfType<IFileProvider>() // Upcast so we can insert on the resulting list.
                .ToList();

            if (additionalFiles.Count == 0)
            {
                return;
            }
            else
            {
                additionalFiles.Insert(0, webRootFileProvider);
                environment.WebRootFileProvider = new CompositeFileProvider(additionalFiles);
            }
        }

        internal static (Stream, bool) ResolveManifest(IWebHostEnvironment environment, IConfiguration configuration)
        {
            try
            {
                var manifestPath = configuration.GetValue<string>(WebHostDefaults.StaticWebAssetsKey);
                var isJson = manifestPath != null && Path.GetExtension(manifestPath).EndsWith(".json", OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                var candidates = manifestPath != null ? new[] { (manifestPath, isJson) } : ResolveRelativeToAssembly(environment);

                foreach (var (candidate, json) in candidates)
                {
                    if (candidate != null && File.Exists(candidate))
                    {
                        return (File.OpenRead(candidate), json);
                    }
                }

                // A missing manifest might simply mean that the feature is not enabled, so we simply
                // return early. Misconfigurations will be uncommon given that the entire process is automated
                // at build time.
                return default;
            }
            catch
            {
                return default;
            }
        }

        private static IEnumerable<(string candidatePath, bool isJson)> ResolveRelativeToAssembly(IWebHostEnvironment environment)
        {
            var assembly = Assembly.Load(environment.ApplicationName);
            var basePath = string.IsNullOrEmpty(assembly.Location) ? AppContext.BaseDirectory : Path.GetDirectoryName(assembly.Location);
            yield return (Path.Combine(basePath!, $"{environment.ApplicationName}.staticwebassets.runtime.json"), isJson: true);
            yield return (Path.Combine(basePath!, $"{environment.ApplicationName}.StaticWebAssets.xml"), isJson: false);
        }
    }
}
#nullable restore
