// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Hosting.StaticWebAssets
{
    // A file provider used for serving static web assets from referenced projects and packages during development.
    // The file provider maps folders from referenced projects and packages and prepends a prefix to their relative
    // paths.
    // At publish time the assets end up in the wwwroot folder of the published app under the prefix indicated here
    // as the base path.
    // For example, for a referenced project mylibrary with content under <<mylibrarypath>>\wwwroot will expose
    // static web assets under _content/mylibrary (this is by convention). The path prefix or base path we apply
    // is that (_content/mylibrary).
    // when the app gets published, the build pipeline puts the static web assets for mylibrary under
    // publish/wwwroot/_content/mylibrary/sample-asset.js
    // To allow for the same experience during development, StaticWebAssetsFileProvider maps the contents of
    // <<mylibrarypath>>\wwwroot\** to _content/mylibrary/**
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

    internal sealed class ManifestStaticWebAssetFileProvider : IFileProvider
    {
        private static readonly StringComparison _fsComparison = OperatingSystem.IsWindows() ?
            StringComparison.OrdinalIgnoreCase :
            StringComparison.Ordinal;

        private static readonly IEqualityComparer<IFileInfo> _nameComparer = new FileNameComparer();

        private readonly IFileProvider[] _fileProviders;
        private readonly StaticWebAssetNode _root;

        public ManifestStaticWebAssetFileProvider(StaticWebAssetManifest manifest, Func<string, IFileProvider> fileProviderFactory)
        {
            _fileProviders = new IFileProvider[manifest.ContentRoots.Length];

            for (int i = 0; i < manifest.ContentRoots.Length; i++)
            {
                _fileProviders[i] = fileProviderFactory(manifest.ContentRoots[i]);
            }

            _root = manifest.Root;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (subpath == null)
            {
                throw new ArgumentNullException(nameof(subpath));
            }

            var segments = Normalize(subpath).Split('/', StringSplitOptions.RemoveEmptyEntries);
            var candidate = _root;

            // Iterate over the path segments until we reach the destination. Whenever we encounter
            // a pattern, we start tracking it as well as the content root directory. On every segment
            // we evalutate the directory to see if there is a subfolder with the current segment and
            // replace it on the dictionary if it exists or remove it if it does not.
            // When we reach our destination we enumerate the files in that folder and evalute them against
            // the pattern to compute the final list.
            HashSet<IFileInfo>? files = null;
            for (var i = 0; i < segments.Length; i++)
            {
                files = GetFilesForCandidatePatterns(segments, candidate, files);

                if (candidate.HasChildren() && candidate.Children.TryGetValue(segments[i], out var child))
                {
                    candidate = child;
                }
                else
                {
                    candidate = null;
                    break;
                }
            }

            if ((candidate == null || (!candidate.HasChildren() && !candidate.HasPatterns())) && files == null)
            {
                return NotFoundDirectoryContents.Singleton;
            }
            else
            {
                // We do this to make sure we account for the patterns on the last segment which are not covered by the loop above
                files = GetFilesForCandidatePatterns(segments, candidate, files);
                if (candidate != null && candidate.HasChildren())
                {
                    files ??= new(_nameComparer);
                    GetCandidateFilesForNode(candidate, files);
                }

                return new StaticWebAssetsDirectoryContents((files as IEnumerable<IFileInfo>) ?? Array.Empty<IFileInfo>());
            }

            HashSet<IFileInfo>? GetFilesForCandidatePatterns(string[] segments, StaticWebAssetNode? candidate, HashSet<IFileInfo>? files)
            {
                if (candidate != null && candidate.HasPatterns())
                {
                    var depth = candidate.Patterns[0].Depth;
                    var candidateDirectoryPath = string.Join('/', segments[depth..]);
                    foreach (var pattern in candidate.Patterns)
                    {
                        var contentRoot = _fileProviders[pattern.ContentRoot];
                        var matcher = new Matcher(_fsComparison);
                        matcher.AddInclude(pattern.Pattern);
                        foreach (var result in contentRoot.GetDirectoryContents(candidateDirectoryPath))
                        {
                            var fileCandidate = string.IsNullOrEmpty(candidateDirectoryPath) ? result.Name : $"{candidateDirectoryPath}/{result.Name}";
                            if (result.Exists && (result.IsDirectory || matcher.Match(fileCandidate).HasMatches))
                            {
                                files ??= new(_nameComparer);
                                if (!files.Contains(result))
                                {
                                    // Multiple patterns might match the same file (even at different locations on disk) at runtime. We don't
                                    // try to disambiguate anything here, since there is already a build step for it. We just pick the first
                                    // file that matches the pattern. The manifest entries are ordered, so while this choice is random, it is
                                    // nonetheless deterministic.
                                    files.Add(result);
                                }
                            }
                        }
                    }
                }

                return files;
            }

            void GetCandidateFilesForNode(StaticWebAssetNode candidate, HashSet<IFileInfo> files)
            {
                foreach (var child in candidate.Children!)
                {
                    var match = child.Value.Match;
                    if (match == null)
                    {
                        // This is a folder
                        var file = new StaticWebAssetsDirectoryInfo(child.Key);
                        // Entries from the manifest always win over any content based on patterns,
                        // so remove any potentially existing file or folder in favor of the manifest
                        // entry.
                        files.Remove(file);
                        files.Add(file);
                    }
                    else
                    {
                        // This is a file.
                        files.RemoveWhere(f => string.Equals(match.Path, f.Name, _fsComparison));
                        var file = _fileProviders[match.ContentRoot].GetFileInfo(match.Path);

                        files.Add(string.Equals(child.Key, match.Path, _fsComparison) ? file :
                            // This means that this file was mapped, there is a chance that we added it to the list
                            // of files by one of the patterns, so we need to replace it with the mapped file.
                            new StaticWebAssetsFileInfo(child.Key, file));
                    }
                }
            }
        }

        private string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (subpath == null)
            {
                throw new ArgumentNullException(nameof(subpath));
            }

            var segments = subpath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            StaticWebAssetNode? candidate = _root;
            List<StaticWebAssetPattern>? patterns = null;

            // Iterate over the path segments until we reach the destination, collecting
            // all pattern candidates along the way except for any pattern at the root.
            for (var i = 0; i < segments.Length; i++)
            {
                if (candidate.HasPatterns())
                {
                    patterns ??= new();
                    patterns.AddRange(candidate.Patterns);
                }
                if (candidate.HasChildren() && candidate.Children.TryGetValue(segments[i], out var child))
                {
                    candidate = child;
                }
                else
                {
                    candidate = null;
                    break;
                }
            }

            var match = candidate?.Match;
            if (match != null)
            {
                // If we found a file, that wins over anything else. If there are conflicts with files added after
                // we've built the manifest, we'll be notified the next time we do a build. This is not different
                // from previous Static Web Assets versions.
                var file = _fileProviders[match.ContentRoot].GetFileInfo(match.Path);
                if (!file.Exists || string.Equals(subpath, Normalize(match.Path), _fsComparison))
                {
                    return file;
                }
                else
                {
                    return new StaticWebAssetsFileInfo(segments[^1], file);
                }
            }

            // The list of patterns is ordered by pattern depth, so we compute the string to check for patterns only
            // once per level. We don't aim to solve conflicts here where multiple files could match a given path,
            // we have a build check that takes care of that.
            var currentDepth = 0;
            var candidatePath = subpath;

            if (patterns != null)
            {
                for (var i = 0; i < patterns.Count; i++)
                {
                    var pattern = patterns[i];
                    if (pattern.Depth != currentDepth)
                    {
                        currentDepth = pattern.Depth;
                        candidatePath = string.Join('/', segments[currentDepth..]);
                    }

                    var result = _fileProviders[pattern.ContentRoot].GetFileInfo(candidatePath);
                    if (result.Exists)
                    {
                        if (!result.IsDirectory)
                        {
                            var matcher = new Matcher();
                            matcher.AddInclude(pattern.Pattern);
                            if (!matcher.Match(candidatePath).HasMatches)
                            {
                                continue;
                            }

                            return result;
                        }
                    }
                }
            }

            return new NotFoundFileInfo(subpath);
        }

        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

        private sealed class StaticWebAssetsDirectoryContents : IDirectoryContents
        {
            private readonly IEnumerable<IFileInfo> _files;

            public StaticWebAssetsDirectoryContents(IEnumerable<IFileInfo> files) =>
                _files = files;

            public bool Exists => true;

            public IEnumerator<IFileInfo> GetEnumerator() => _files.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StaticWebAssetsDirectoryInfo : IFileInfo
        {
            private static readonly DateTimeOffset _lastModified = DateTimeOffset.FromUnixTimeSeconds(0);

            public StaticWebAssetsDirectoryInfo(string name)
            {
                Name = name;
            }

            public bool Exists => true;

            public long Length => 0;

            public string? PhysicalPath => null;

            public DateTimeOffset LastModified => _lastModified;

            public bool IsDirectory => true;

            public string Name { get; }

            public Stream CreateReadStream() => throw new InvalidOperationException("Can not create a stream for a directory.");
        }

        private sealed class StaticWebAssetsFileInfo : IFileInfo
        {
            private readonly IFileInfo _source;

            public StaticWebAssetsFileInfo(string name, IFileInfo source)
            {
                Name = name;
                _source = source;
            }
            public bool Exists => _source.Exists;

            public long Length => _source.Length;

            public string PhysicalPath => _source.PhysicalPath;

            public DateTimeOffset LastModified => _source.LastModified;

            public bool IsDirectory => _source.IsDirectory;

            public string Name { get; }

            public Stream CreateReadStream() => _source.CreateReadStream();
        }

        private sealed class FileNameComparer : IEqualityComparer<IFileInfo>
        {
            public bool Equals(IFileInfo? x, IFileInfo? y) => string.Equals(x?.Name, y?.Name, _fsComparison);

            public int GetHashCode(IFileInfo obj) => obj.Name.GetHashCode(_fsComparison);
        }

        internal sealed class StaticWebAssetManifest
        {
            internal static readonly StringComparer PathComparer =
                OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

            public string[] ContentRoots { get; set; } = Array.Empty<string>();

            public StaticWebAssetNode Root { get; set; } = null!;

            internal static StaticWebAssetManifest Parse(Stream manifest)
            {
                return JsonSerializer.Deserialize<StaticWebAssetManifest>(manifest)!;
            }
        }

        internal sealed class StaticWebAssetNode
        {
            [JsonPropertyName("Asset")]
            public StaticWebAssetMatch? Match { get; set; }

            [JsonConverter(typeof(OSBasedCaseConverter))]
            public Dictionary<string, StaticWebAssetNode>? Children { get; set; }

            public StaticWebAssetPattern[]? Patterns { get; set; }

            [MemberNotNullWhen(true, nameof(Children))]
            internal bool HasChildren() => Children != null && Children.Count > 0;

            [MemberNotNullWhen(true, nameof(Patterns))]
            internal bool HasPatterns() => Patterns != null && Patterns.Length > 0;
        }

        internal sealed class StaticWebAssetMatch
        {
            [JsonPropertyName("ContentRootIndex")]
            public int ContentRoot { get; set; }

            [JsonPropertyName("SubPath")]
            public string Path { get; set; } = null!;
        }

        internal sealed class StaticWebAssetPattern
        {
            [JsonPropertyName("ContentRootIndex")]
            public int ContentRoot { get; set; }

            public int Depth { get; set; }

            public string Pattern { get; set; } = null!;
        }

        private sealed class OSBasedCaseConverter : JsonConverter<Dictionary<string, StaticWebAssetNode>>
        {
            public override Dictionary<string, StaticWebAssetNode> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new Dictionary<string, StaticWebAssetNode>(
                    JsonSerializer.Deserialize<IDictionary<string, StaticWebAssetNode>>(ref reader, options)!,
                    StaticWebAssetManifest.PathComparer);
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, StaticWebAssetNode> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }
    }
}
