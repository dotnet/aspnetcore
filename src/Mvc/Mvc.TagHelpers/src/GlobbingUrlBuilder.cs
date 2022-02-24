// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// Utility methods for <see cref="AspNetCore.Razor.TagHelpers.ITagHelper"/>'s that support
/// attributes containing file globbing patterns.
/// </summary>
public class GlobbingUrlBuilder
{
    // Valid whitespace characters defined by the HTML5 spec.
    private static readonly char[] ValidAttributeWhitespaceChars =
        new[] { '\t', '\n', '\u000C', '\r', ' ' };
    private static readonly PathComparer DefaultPathComparer = new PathComparer();
    private static readonly char[] PatternSeparator = new[] { ',' };
    private static readonly char[] PathSeparator = new[] { '/' };
    private readonly FileProviderGlobbingDirectory _baseGlobbingDirectory;

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

        if (cache == null)
        {
            throw new ArgumentNullException(nameof(cache));
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
    public virtual IReadOnlyList<string> BuildUrlList(
        string staticUrl,
        string includePattern,
        string excludePattern)
    {
        // Get urls that match the globbing patterns specified
        var globbedUrls = ExpandGlobbedUrl(includePattern, excludePattern);

        if (staticUrl == null)
        {
            return globbedUrls;
        }

        // The staticUrl always appears first in the sequence.
        var urls = new List<string>(1 + globbedUrls.Count)
            {
                staticUrl
            };

        for (var i = 0; i < globbedUrls.Count; i++)
        {
            if (!string.Equals(staticUrl, globbedUrls[i], StringComparison.Ordinal))
            {
                urls.Add(globbedUrls[i]);
            }
        }

        return urls;
    }

    private IReadOnlyList<string> ExpandGlobbedUrl(string include, string exclude)
    {
        if (string.IsNullOrEmpty(include))
        {
            return Array.Empty<string>();
        }

        var cacheKey = new GlobbingUrlKey(include, exclude);
        if (Cache.TryGetValue(cacheKey, out List<string> files))
        {
            return files;
        }

        var includeTokenizer = new StringTokenizer(include, PatternSeparator);
        var includeEnumerator = includeTokenizer.GetEnumerator();
        if (!includeEnumerator.MoveNext())
        {
            return Array.Empty<string>();
        }

        var options = new MemoryCacheEntryOptions();
        var trimmedIncludePatterns = new List<string>();
        foreach (var includePattern in includeTokenizer)
        {
            var changeToken = FileProvider.Watch(includePattern.Value);
            options.AddExpirationToken(changeToken);
            trimmedIncludePatterns.Add(NormalizePath(includePattern));
        }
        var matcher = MatcherBuilder != null ? MatcherBuilder() : new Matcher();
        matcher.AddIncludePatterns(trimmedIncludePatterns);

        if (!string.IsNullOrWhiteSpace(exclude))
        {
            var excludeTokenizer = new StringTokenizer(exclude, PatternSeparator);
            var trimmedExcludePatterns = new List<string>();
            foreach (var excludePattern in excludeTokenizer)
            {
                trimmedExcludePatterns.Add(NormalizePath(excludePattern));
            }
            matcher.AddExcludePatterns(trimmedExcludePatterns);
        }

        var (matchedUrls, sizeInBytes) = FindFiles(matcher);
        options.SetSize(sizeInBytes);

        return Cache.Set(
            cacheKey,
            matchedUrls,
            options);
    }

    private (List<string> matchedUrls, long sizeInBytes) FindFiles(Matcher matcher)
    {
        var matches = matcher.Execute(_baseGlobbingDirectory);
        var matchedUrls = new List<string>();
        var sizeInBytes = 0L;

        foreach (var matchedPath in matches.Files)
        {
            // Resolve the path to site root
            var relativePath = new PathString("/" + matchedPath.Path);
            var matchedUrl = RequestPathBase.Add(relativePath).ToString();
            var index = matchedUrls.BinarySearch(matchedUrl, DefaultPathComparer);
            if (index < 0)
            {
                // Item doesn't already exist. Insert it.
                matchedUrls.Insert(~index, matchedUrl);
                sizeInBytes += matchedUrl.Length * sizeof(char);
            }
        }

        return (matchedUrls, sizeInBytes);
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
            var xLength = xExtIndex >= 0 ? xExtIndex : x.Length;
            var yLength = yExtIndex >= 0 ? yExtIndex : y.Length;
            var compareLength = Math.Max(xLength, yLength);

            // In the resulting sequence, we want shorter paths to appear prior to longer paths. For paths of equal
            // depth, we'll compare individual segments. The first segment that differs determines the result.
            // For e.g.
            // Foo.cshtml < Foo.xhtml
            // Bar.cshtml < Foo.cshtml
            // ZZ/z.txt < A/A/a.txt
            // ZZ/a/z.txt < ZZ/z/a.txt

            if (string.Compare(x, 0, y, 0, compareLength, StringComparison.Ordinal) == 0)
            {
                // Only extension differs so just compare the extension
                if (xExtIndex >= 0 && yExtIndex >= 0)
                {
                    var length = x.Length - xExtIndex;
                    return string.Compare(x, xExtIndex, y, yExtIndex, length, StringComparison.Ordinal);
                }

                return xExtIndex - yExtIndex;
            }

            var xNoExt = xExtIndex >= 0 ? x.Substring(0, xExtIndex) : x;
            var yNoExt = yExtIndex >= 0 ? y.Substring(0, yExtIndex) : y;

            var result = 0;
            var xEnumerator = new StringTokenizer(xNoExt, PathSeparator).GetEnumerator();
            var yEnumerator = new StringTokenizer(yNoExt, PathSeparator).GetEnumerator();
            while (TryGetNextSegment(ref xEnumerator, out var xSegment))
            {
                if (!TryGetNextSegment(ref yEnumerator, out var ySegment))
                {
                    // Different path depths (right is shorter), so shallower path wins.
                    return 1;
                }

                if (result != 0)
                {
                    // Once we've determined that a segment differs, we need to ensure that the two paths
                    // are of equal depth.
                    continue;
                }

                var length = Math.Max(xSegment.Length, ySegment.Length);
                result = string.Compare(
                    xSegment.Buffer,
                    xSegment.Offset,
                    ySegment.Buffer,
                    ySegment.Offset,
                    length,
                    StringComparison.Ordinal);
            }

            if (TryGetNextSegment(ref yEnumerator, out _))
            {
                // Different path depths (left is shorter). Shallower path wins.
                return -1;
            }
            else
            {
                // Segments are of equal length
                return result;
            }
        }

        private static bool TryGetNextSegment(ref StringTokenizer.Enumerator enumerator, out StringSegment segment)
        {
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.HasValue && enumerator.Current.Length > 0)
                {
                    segment = enumerator.Current;
                    return true;
                }
            }

            segment = default(StringSegment);
            return false;
        }
    }

    private static string NormalizePath(StringSegment value)
    {
        if (!value.HasValue || value.Length == 0)
        {
            return null;
        }

        value = Trim(value);
        if (value.StartsWith("~/", StringComparison.Ordinal))
        {
            value = new StringSegment(value.Buffer, value.Offset + 2, value.Length - 2);
        }
        else if (value.StartsWith("/", StringComparison.Ordinal) ||
            value.StartsWith("\\", StringComparison.Ordinal))
        {
            // Trim the leading slash as the matcher runs from the provided root only anyway
            value = new StringSegment(value.Buffer, value.Offset + 1, value.Length - 1);
        }

        return value.Value;
    }

    private static bool IsWhiteSpace(string value, int index)
    {
        for (var i = 0; i < ValidAttributeWhitespaceChars.Length; i++)
        {
            if (value[index] == ValidAttributeWhitespaceChars[i])
            {
                return true;
            }
        }

        return false;
    }

    private static StringSegment Trim(StringSegment value)
    {
        var offset = value.Offset;
        while (offset < value.Offset + value.Length)
        {
            if (!IsWhiteSpace(value.Buffer, offset))
            {
                break;
            }

            offset++;
        }

        var trimmedEnd = value.Offset + value.Length - 1;
        while (trimmedEnd >= offset)
        {
            if (!IsWhiteSpace(value.Buffer, trimmedEnd))
            {
                break;
            }

            trimmedEnd--;
        }

        return new StringSegment(value.Buffer, offset, trimmedEnd - offset + 1);
    }

    private readonly struct GlobbingUrlKey : IEquatable<GlobbingUrlKey>
    {
        public GlobbingUrlKey(string include, string exclude)
        {
            Include = include;
            Exclude = exclude;
        }

        public string Include { get; }

        public string Exclude { get; }

        public bool Equals(GlobbingUrlKey other)
        {
            return string.Equals(Include, other.Include, StringComparison.Ordinal) &&
                string.Equals(Exclude, other.Exclude, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Include, Exclude);
        }
    }
}
