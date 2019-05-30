// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore
{
    internal class StaticWebAssetsLoader
    {
        internal const string StaticWebAssetsManifestName = "Microsoft.AspNetCore.StaticWebAssets.xml";

        internal static void UseStaticWebAssets(IWebHostEnvironment environment)
        {
            using (var manifest = ResolveManifest(environment))
            {
                if (manifest != null)
                {
                    UseStaticWebAssetsCore(environment, manifest);
                }
            }
        }

        internal static void UseStaticWebAssetsCore(IWebHostEnvironment environment, Stream manifest)
        {
            var staticWebAssetsFileProvider = new List<IFileProvider>();
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

        internal static Stream ResolveManifest(IWebHostEnvironment environment)
        {
            // We plan to remove the embedded file resolution code path in
            // a future preview.
            Assembly assembly = null;
            try
            {
                assembly = Assembly.Load(environment.ApplicationName);
            }
            catch (Exception)
            {
            }

            if (assembly != null && assembly.GetManifestResourceNames().Any(a => a == StaticWebAssetsManifestName))
            {
                return assembly.GetManifestResourceStream(StaticWebAssetsManifestName);
            }
            else
            {
                // Fallback to physical file as we plan to use a file on disk instead of the embedded resource.
                var filePath = Path.Combine(Path.GetDirectoryName(GetAssemblyLocation(assembly)), $"{environment.ApplicationName}.StaticWebAssets.xml");
                if (File.Exists(filePath))
                {
                    return File.OpenRead(filePath);
                }
                else
                {
                    // A missing manifest might simply mean that the feature is not enabled, so we simply
                    // return early. Misconfigurations will be uncommon given that the entire process is automated
                    // at build time.
                    return null;
                }
            }
        }

        internal static string GetAssemblyLocation(Assembly assembly)
        {
            if (Uri.TryCreate(assembly.CodeBase, UriKind.Absolute, out var result) &&
                result.IsFile && string.IsNullOrWhiteSpace(result.Fragment))
            {
                return result.LocalPath;
            }

            return assembly.Location;
        }
    }
}
