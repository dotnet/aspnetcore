// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.WebView
{
    internal class StaticWebAssetsLoader
    {
        internal const string StaticWebAssetsManifestName = "Microsoft.AspNetCore.StaticWebAssets.xml";

        internal static IFileProvider UseStaticWebAssets(IFileProvider systemProvider)
        {
            using var manifest = GetManifestStream();
            if (manifest != null)
            {
                return UseStaticWebAssetsCore(systemProvider, manifest);
            }
            else
            {
                return systemProvider;
            }

            static Stream? GetManifestStream()
            {
                try
                {
                    var filePath = ResolveRelativeToAssembly();

                    if (filePath != null && File.Exists(filePath))
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
                catch
                {
                    return null;
                }
            }
        }

        internal static IFileProvider UseStaticWebAssetsCore(IFileProvider systemProvider, Stream manifest)
        {
            var webRootFileProvider = systemProvider;

            var additionalFiles = StaticWebAssetsReader.Parse(manifest)
                .Select(cr => new StaticWebAssetsFileProvider(cr.BasePath, cr.Path))
                .OfType<IFileProvider>() // Upcast so we can insert on the resulting list.
                .ToList();

            if (additionalFiles.Count == 0)
            {
                return systemProvider;
            }
            else
            {
                additionalFiles.Insert(0, webRootFileProvider);
                return new CompositeFileProvider(additionalFiles);
            }
        }

        private static string? ResolveRelativeToAssembly()
        {
            var assembly = Assembly.GetEntryAssembly();
            if (string.IsNullOrEmpty(assembly?.Location))
            {
                return null;
            }

            var name = Path.GetFileNameWithoutExtension(assembly.Location);

            return Path.Combine(Path.GetDirectoryName(assembly.Location)!, $"{name}.StaticWebAssets.xml");
        }

        internal static class StaticWebAssetsReader
        {
            private const string ManifestRootElementName = "StaticWebAssets";
            private const string VersionAttributeName = "Version";
            private const string ContentRootElementName = "ContentRoot";

            internal static IEnumerable<ContentRootMapping> Parse(Stream manifest)
            {
                var document = XDocument.Load(manifest);
                if (!string.Equals(document.Root!.Name.LocalName, ManifestRootElementName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Invalid manifest format. Manifest root must be '{ManifestRootElementName}'");
                }

                var version = document.Root.Attribute(VersionAttributeName);
                if (version == null)
                {
                    throw new InvalidOperationException($"Invalid manifest format. Manifest root element must contain a version '{VersionAttributeName}' attribute");
                }

                if (version.Value != "1.0")
                {
                    throw new InvalidOperationException($"Unknown manifest version. Manifest version must be '1.0'");
                }

                foreach (var element in document.Root.Elements())
                {
                    if (!string.Equals(element.Name.LocalName, ContentRootElementName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Invalid manifest format. Invalid element '{element.Name.LocalName}'. All {StaticWebAssetsLoader.StaticWebAssetsManifestName} child elements must be '{ContentRootElementName}' elements.");
                    }
                    if (!element.IsEmpty)
                    {
                        throw new InvalidOperationException($"Invalid manifest format. {ContentRootElementName} can't have content.");
                    }

                    var basePath = ParseRequiredAttribute(element, "BasePath");
                    var path = ParseRequiredAttribute(element, "Path");
                    yield return new ContentRootMapping(basePath, path);
                }
            }

            private static string ParseRequiredAttribute(XElement element, string attributeName)
            {
                var attribute = element.Attribute(attributeName);
                if (attribute == null)
                {
                    throw new InvalidOperationException($"Invalid manifest format. Missing {attributeName} attribute in '{ContentRootElementName}' element.");
                }
                return attribute.Value;
            }

            internal readonly struct ContentRootMapping
            {
                public ContentRootMapping(string basePath, string path)
                {
                    BasePath = basePath;
                    Path = path;
                }

                public string BasePath { get; }
                public string Path { get; }
            }
        }

        internal class StaticWebAssetsFileProvider : IFileProvider
        {
            private static readonly StringComparison FilePathComparison = OperatingSystem.IsWindows() ?
                StringComparison.OrdinalIgnoreCase :
                StringComparison.Ordinal;

            public StaticWebAssetsFileProvider(string pathPrefix, string contentRoot)
            {
                BasePath = NormalizePath(pathPrefix);
                InnerProvider = new PhysicalFileProvider(contentRoot);
            }

            public PhysicalFileProvider InnerProvider { get; }

            public PathString BasePath { get; }

            /// <inheritdoc />
            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                var modifiedSub = NormalizePath(subpath);

                if (BasePath == "/")
                {
                    return InnerProvider.GetDirectoryContents(modifiedSub);
                }

                if (StartsWithBasePath(modifiedSub, out var physicalPath))
                {
                    return InnerProvider.GetDirectoryContents(physicalPath.Value);
                }
                else if (string.Equals(subpath, string.Empty) || string.Equals(modifiedSub, "/"))
                {
                    return new StaticWebAssetsDirectoryRoot(BasePath);
                }
                else if (BasePath.StartsWithSegments(modifiedSub, FilePathComparison, out var remaining))
                {
                    return new StaticWebAssetsDirectoryRoot(remaining);
                }

                return NotFoundDirectoryContents.Singleton;
            }

            /// <inheritdoc />
            public IFileInfo GetFileInfo(string subpath)
            {
                var modifiedSub = NormalizePath(subpath);

                if (BasePath == "/")
                {
                    return InnerProvider.GetFileInfo(subpath);
                }

                if (!StartsWithBasePath(modifiedSub, out var physicalPath))
                {
                    return new NotFoundFileInfo(subpath);
                }
                else
                {
                    return InnerProvider.GetFileInfo(physicalPath.Value);
                }
            }

            /// <inheritdoc />
            public IChangeToken Watch(string filter)
            {
                return InnerProvider.Watch(filter);
            }

            private static string NormalizePath(string path)
            {
                path = path.Replace('\\', '/');
                return path.StartsWith('/') ? path : "/" + path;
            }

            private bool StartsWithBasePath(string subpath, out PathString rest)
            {
                return new PathString(subpath).StartsWithSegments(BasePath, FilePathComparison, out rest);
            }

            private class StaticWebAssetsDirectoryRoot : IDirectoryContents
            {
                private readonly string _nextSegment;

                public StaticWebAssetsDirectoryRoot(PathString remainingPath)
                {
                    // We MUST use the Value property here because it is unescaped.
                    _nextSegment = remainingPath.Value?.Split("/", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                }

                public bool Exists => true;

                public IEnumerator<IFileInfo> GetEnumerator()
                {
                    return GenerateEnum();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GenerateEnum();
                }

                private IEnumerator<IFileInfo> GenerateEnum()
                {
                    return new[] { new StaticWebAssetsFileInfo(_nextSegment) }
                        .Cast<IFileInfo>().GetEnumerator();
                }

                private class StaticWebAssetsFileInfo : IFileInfo
                {
                    public StaticWebAssetsFileInfo(string name)
                    {
                        Name = name;
                    }

                    public bool Exists => true;

                    public long Length => throw new NotImplementedException();

                    public string PhysicalPath => throw new NotImplementedException();

                    public DateTimeOffset LastModified => throw new NotImplementedException();

                    public bool IsDirectory => true;

                    public string Name { get; }

                    public Stream CreateReadStream()
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
#nullable restore
