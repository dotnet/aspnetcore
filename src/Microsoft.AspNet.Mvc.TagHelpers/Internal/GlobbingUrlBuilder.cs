// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// Utility methods for <see cref="AspNet.Razor.TagHelpers.ITagHelper"/>'s that support
    /// attributes containing file globbing patterns.
    /// </summary>
    public class GlobbingUrlBuilder
    {
        private static readonly char[] PatternSeparator = new[] { ',' };

        // Valid whitespace characters defined by the HTML5 spec.
        private static readonly char[] ValidAttributeWhitespaceChars =
            new[] { '\t', '\n', '\u000C', '\r', ' ' };

        private static readonly PathComparer DefaultPathComparer = new PathComparer();

        private readonly FileProviderGlobbingDirectory _baseGlobbingDirectory;

        public GlobbingUrlBuilder() { }

        /// <summary>
        /// Creates a new <see cref="GlobbingUrlBuilder"/>.
        /// </summary>
        /// <param name="fileProvider">The file provider.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="requestPathBase">The request path base.</param>
        public GlobbingUrlBuilder(IFileProvider fileProvider, IMemoryCache cache, PathString requestPathBase)
        {
            if (fileProvider == null)
            {
                throw new ArgumentNullException(nameof(fileProvider));
            }

            FileProvider = fileProvider;
            Cache = cache;
            RequestPathBase = requestPathBase;
            _baseGlobbingDirectory = new FileProviderGlobbingDirectory(fileProvider, fileInfo: null, parent: null);
        }

        /// <summary>
        /// The <see cref="IMemoryCache"/> to cache globbing results in.
        /// </summary>
        public IMemoryCache Cache { get; }

        /// <summary>
        /// The <see cref="IFileProvider"/> used to watch for changes to file globbing results.
        /// </summary>
        public IFileProvider FileProvider { get; }

        /// <summary>
        /// The base path of the current request (i.e. <see cref="HttpRequest.PathBase"/>).
        /// </summary>
        public PathString RequestPathBase { get; }

        // Internal for testing.
        internal Func<Matcher> MatcherBuilder { get; set; }

        /// <summary>
        /// Builds a list of URLs.
        /// </summary>
        /// <param name="staticUrl">The statically declared URL. This will always be added to the result.</param>
        /// <param name="includePattern">The file globbing include pattern.</param>
        /// <param name="excludePattern">The file globbing exclude pattern.</param>
        /// <returns>The list of URLs</returns>
        public virtual ICollection<string> BuildUrlList(string staticUrl, string includePattern, string excludePattern)
        {
            var urls = new HashSet<string>(StringComparer.Ordinal);

            // Add the statically declared url if present
            if (staticUrl != null)
            {
                urls.Add(staticUrl);
            }

            // Add urls that match the globbing patterns specified
            var matchedUrls = ExpandGlobbedUrl(includePattern, excludePattern);
            urls.UnionWith(matchedUrls);

            return urls;
        }

        private IEnumerable<string> ExpandGlobbedUrl(string include, string exclude)
        {
            if (string.IsNullOrEmpty(include))
            {
                return Enumerable.Empty<string>();
            }

            var includePatterns = include.Split(PatternSeparator, StringSplitOptions.RemoveEmptyEntries);
            var excludePatterns = exclude?.Split(PatternSeparator, StringSplitOptions.RemoveEmptyEntries);

            if (includePatterns.Length == 0)
            {
                return Enumerable.Empty<string>();
            }

            if (Cache != null)
            {
                var cacheKey = $"{nameof(GlobbingUrlBuilder)}-inc:{include}-exc:{exclude}";
                IEnumerable<string> files;
                if (!Cache.TryGetValue(cacheKey, out files))
                {
                    var options = new MemoryCacheEntryOptions();

                    for (var i = 0; i < includePatterns.Length; i++)
                    {
                        var changeToken = FileProvider.Watch(includePatterns[i]);
                        options.AddExpirationToken(changeToken);
                    }

                    files = FindFiles(includePatterns, excludePatterns);

                    Cache.Set(cacheKey, files, options);
                }
                return files;
            }

            return FindFiles(includePatterns, excludePatterns);
        }

        private IEnumerable<string> FindFiles(string[] includePatterns, string[] excludePatterns)
        {
            var matcher = MatcherBuilder != null ? MatcherBuilder() : new Matcher();
            var trimmedIncludePatterns = new List<string>();
            for (var i = 0; i < includePatterns.Length; i++)
            {
                trimmedIncludePatterns.Add(TrimLeadingTildeSlash(includePatterns[i]));
            }
            matcher.AddIncludePatterns(trimmedIncludePatterns);

            if (excludePatterns != null)
            {
                var trimmedExcludePatterns = new List<string>();
                for (var i = 0; i < excludePatterns.Length; i++)
                {
                    trimmedExcludePatterns.Add(TrimLeadingTildeSlash(excludePatterns[i]));
                }
                matcher.AddExcludePatterns(trimmedExcludePatterns);
            }

            var matches = matcher.Execute(_baseGlobbingDirectory);

            return matches.Files.Select(ResolveMatchedPath)
                .OrderBy(path => path, DefaultPathComparer);
        }

        private string ResolveMatchedPath(FilePatternMatch matchedPath)
        {
            // Resolve the path to site root
            var relativePath = new PathString("/" + matchedPath.Path);
            return RequestPathBase.Add(relativePath).ToString();
        }

        private class PathComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                // < 0 = x < y
                // > 0 = x > y

                if (string.Equals(x, y, StringComparison.Ordinal))
                {
                    return 0;
                }

                if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y))
                {
                    return string.Compare(x, y, StringComparison.Ordinal);
                }

                var xExtIndex = x.LastIndexOf('.');
                var yExtIndex = y.LastIndexOf('.');

                // Ensure extension index is in the last segment, i.e. in the file name
                var xSlashIndex = x.LastIndexOf('/');
                var ySlashIndex = y.LastIndexOf('/');
                xExtIndex = xExtIndex > xSlashIndex ? xExtIndex : -1;
                yExtIndex = yExtIndex > ySlashIndex ? yExtIndex : -1;

                // Get paths without their extensions, if they have one
                var xNoExt = xExtIndex >= 0 ? x.Substring(0, xExtIndex) : x;
                var yNoExt = yExtIndex >= 0 ? y.Substring(0, yExtIndex) : y;

                if (string.Equals(xNoExt, yNoExt, StringComparison.Ordinal))
                {
                    // Only extension differs so just compare the extension
                    var xExt = xExtIndex >= 0 ? x.Substring(xExtIndex) : string.Empty;
                    var yExt = yExtIndex >= 0 ? y.Substring(yExtIndex) : string.Empty;
                    return string.Compare(xExt, yExt, StringComparison.Ordinal);
                }

                var xSegments = xNoExt.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var ySegments = yNoExt.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (xSegments.Length != ySegments.Length)
                {
                    // Different path depths so shallower path wins
                    return xSegments.Length.CompareTo(ySegments.Length);
                }

                // Depth is the same so compare each segment
                for (int i = 0; i < xSegments.Length; i++)
                {
                    var xSegment = xSegments[i];
                    var ySegment = ySegments[i];

                    var xToY = string.Compare(xSegment, ySegment, StringComparison.Ordinal);
                    if (xToY != 0)
                    {
                        return xToY;
                    }
                }

                // Should't get here, but if we do, hey, they're the same :)
                return 0;
            }
        }

        private static string TrimLeadingTildeSlash(string value)
        {
            var result = value.Trim(ValidAttributeWhitespaceChars);

            if (result.StartsWith("~/", StringComparison.Ordinal))
            {
                result = result.Substring(2);
            }
            else if (result.StartsWith("/", StringComparison.Ordinal) ||
                result.StartsWith("\\", StringComparison.Ordinal))
            {
                // Trim the leading slash as the matcher runs from the provided root only anyway
                result = result.Substring(1);
            }

            return result;
        }
    }
}