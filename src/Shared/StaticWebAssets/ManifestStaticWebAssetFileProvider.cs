// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.StaticWebAssets;

internal sealed partial class ManifestStaticWebAssetFileProvider : IFileProvider
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

    // For testing purposes only
    internal IFileProvider[] FileProviders => _fileProviders;

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        ArgumentNullException.ThrowIfNull(subpath);

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

    private static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        ArgumentNullException.ThrowIfNull(subpath);

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
        var currentDepth = -1;
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

        public string PhysicalPath => _source.PhysicalPath ?? string.Empty;

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
            return JsonSerializer.Deserialize(
                manifest,
                SourceGenerationContext.DefaultWithConverter.StaticWebAssetManifest)!;
        }
    }

    [JsonSourceGenerationOptions]
    [JsonSerializable(typeof(StaticWebAssetManifest))]
    [JsonSerializable(typeof(IDictionary<string, StaticWebAssetNode>))]
    internal sealed partial class SourceGenerationContext : JsonSerializerContext
    {
        public static readonly SourceGenerationContext DefaultWithConverter = new SourceGenerationContext(new JsonSerializerOptions
        {
            Converters = { new OSBasedCaseConverter() }
        });
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
            // Need to recursively deserialize `Dictionary<string, StaticWebAssetNode>` but can't deserialize
            // that type directly because this converter will call into itself and stackoverflow.
            // Workaround is to deserialize to IDictionary, and then perform custom convert logic on the result.
            var parsed = JsonSerializer.Deserialize(ref reader, SourceGenerationContext.DefaultWithConverter.IDictionaryStringStaticWebAssetNode)!;
            var result = new Dictionary<string, StaticWebAssetNode>(StaticWebAssetManifest.PathComparer);
            MergeChildren(parsed, result);
            return result;

            static void MergeChildren(
                IDictionary<string, StaticWebAssetNode> newChildren,
                IDictionary<string, StaticWebAssetNode> existing)
            {
                foreach (var (key, value) in newChildren)
                {
                    if (!existing.TryGetValue(key, out var existingNode))
                    {
                        existing.Add(key, value);
                    }
                    else
                    {
                        if (value.Patterns != null)
                        {
                            if (existingNode.Patterns == null)
                            {
                                existingNode.Patterns = value.Patterns;
                            }
                            else
                            {
                                if (value.Patterns.Length > 0)
                                {
                                    var newList = new StaticWebAssetPattern[existingNode.Patterns.Length + value.Patterns.Length];
                                    existingNode.Patterns.CopyTo(newList, 0);
                                    value.Patterns.CopyTo(newList, existingNode.Patterns.Length);
                                    existingNode.Patterns = newList;
                                }
                            }
                        }

                        if (value.Children != null)
                        {
                            if (existingNode.Children == null)
                            {
                                existingNode.Children = value.Children;
                            }
                            else
                            {
                                if (value.Children.Count > 0)
                                {
                                    MergeChildren(value.Children, existingNode.Children);
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, StaticWebAssetNode> value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
