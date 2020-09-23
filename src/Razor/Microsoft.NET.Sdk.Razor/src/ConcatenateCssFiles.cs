// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class ConcatenateCssFiles : Task
    {
        private static readonly IComparer<ITaskItem> _fullPathComparer =
            Comparer<ITaskItem>.Create((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.GetMetadata("FullPath"), y.GetMetadata("FullPath")));

        [Required]
        public ITaskItem[] ScopedCssFiles { get; set; }

        [Required]
        public ITaskItem[] ProjectBundles { get; set; }

        [Required]
        public string ScopedCssBundleBasePath { get; set; }

        [Required]
        public string OutputFile { get; set; }

        public override bool Execute()
        {
            if (ProjectBundles.Length > 0)
            {
                Array.Sort(ProjectBundles, _fullPathComparer);
            }
            Array.Sort(ScopedCssFiles, _fullPathComparer);

            var builder = new StringBuilder();
            if (ProjectBundles.Length > 0)
            {
                // We are importing bundles from other class libraries and packages, in that case we need to compute the
                // import path relative to the position of where the final bundle will be.
                // Our final bundle will always be at "<<CurrentBasePath>>/scoped.styles.css"
                // Other bundles will be at "<<BundleBasePath>>/bundle.bdl.scp.css"
                // The base and relative paths can be modified by the user, so we do a normalization process to ensure they
                // are in the shape we expect them before we use them.
                // We normalize path separators to '\' from '/' which is what we expect on a url. The separator can come as
                // '\' as a result of user input or another MSBuild path normalization operation. We always want '/' since that
                // is what is valid on the url.
                // We remove leading and trailing '/' on all paths to ensure we can combine them properly. Users might specify their
                // base path with or without forward and trailing slashes and we always need to make sure we combine them appropriately.
                // These links need to be relative to the final bundle to be independent of the path where the main app is being served.
                // For example:
                // An app is served from the "subdir" path base, the main bundle path on disk is "MyApp/scoped.styles.css" and it uses a
                // library with scoped components that is placed on "_content/library/bundle.bdl.scp.css".
                // The resulting import would be "import '../_content/library/bundle.bdl.scp.css'".
                // If we were to produce "/_content/library/bundle.bdl.scp.css" it would fail to accoutn for "subdir"
                // We could produce shorter paths if we detected common segments between the final bundle base path and the imported bundle
                // base paths, but its more work and it will not have a significant impact on the bundle size size.
                var normalizedBasePath = NormalizePath(ScopedCssBundleBasePath);
                var currentBasePathSegments = normalizedBasePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var prefix = string.Join("/", Enumerable.Repeat("..", currentBasePathSegments.Length));
                for (var i = 0; i < ProjectBundles.Length; i++)
                {
                    var bundle = ProjectBundles[i];
                    var bundleBasePath = NormalizePath(bundle.GetMetadata("BasePath"));
                    var relativePath = NormalizePath(bundle.GetMetadata("RelativePath"));
                    var importPath = NormalizePath(Path.Combine(prefix, bundleBasePath, relativePath));

                    builder.AppendLine($"@import '{importPath}';");
                }

                builder.AppendLine();
            }

            for (var i = 0; i < ScopedCssFiles.Length; i++)
            {
                var current = ScopedCssFiles[i];
                builder.AppendLine($"/* {NormalizePath(current.GetMetadata("BasePath"))}/{NormalizePath(current.GetMetadata("RelativePath"))} */");
                foreach (var line in File.ReadLines(current.GetMetadata("FullPath")))
                {
                    builder.AppendLine(line);
                }
            }

            var content = builder.ToString();

            if (!File.Exists(OutputFile) || !SameContent(content, OutputFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));
                File.WriteAllText(OutputFile, content);
            }


            return !Log.HasLoggedErrors;
        }

        private string NormalizePath(string path) => path.Replace("\\", "/").Trim('/');

        private bool SameContent(string content, string outputFilePath)
        {
            var contentHash = GetContentHash(content);

            var outputContent = File.ReadAllText(outputFilePath);
            var outputContentHash = GetContentHash(outputContent);

            for (int i = 0; i < outputContentHash.Length; i++)
            {
                if (outputContentHash[i] != contentHash[i])
                {
                    return false;
                }
            }

            return true;

            static byte[] GetContentHash(string content)
            {
                using var sha256 = SHA256.Create();
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            }
        }
    }
}
