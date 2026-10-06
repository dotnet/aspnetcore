// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

#if !PREPARE_REVIEW_TESTS
return await PrepareReviewProgram.RunAsync(args);
#endif

internal static partial class PrepareReviewProgram
{
    internal const string Suffix = ".source";
    internal const string RoutingPath = ".github/skills/review-pull-request/routing.md";
    private const int MaximumBlobBytes = 16 * 1024 * 1024;
    private const int MaximumProcessOutputBytes = 64 * 1024 * 1024;
    private const string TargetMovedDuringPreparationMessage =
        "The target or base branch moved during preparation; no ready manifest was written.";
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    internal sealed record Options(
        string? Repo,
        string? Pr,
        string Output,
        string? Head = null,
        string? Hostname = null,
        string? Guidance = null,
        string? GuidanceRoot = null,
        bool Check = false);

    internal sealed record Dependencies(
        Func<string, string?, Task<JsonNode?>>? Api = null,
        Func<string, IReadOnlyList<string>, string, Task>? Fetch = null,
        string? ProducerSourcePath = null);

    internal sealed record Link(string Path, string? Anchor, string Guide, string? Role = null, string? Reason = null);
    internal sealed record GuideLinkResult(List<Link> Included, List<Link> Context, List<Link> Skipped);
    internal sealed record GuideResult(string Principles, List<string> Topics, string Body);
    internal sealed record RoutingEntry(string Prefix, string Guide);
    private sealed record RoutingResult(string Sha256, List<string> Guides);
    private sealed record TreeEntry(string Mode, string Type, string Sha, string Name);
    private sealed record Pointer(string Path, string Kind, string? Commit = null);

    internal static async Task<int> RunAsync(string[] args, Dependencies? dependencies = null)
    {
        try
        {
            var parsed = ParseArguments(args);
            var repo = parsed.Repo;
            if (repo is null)
            {
                var remote = Utf8NoBom.GetString(Run("git", GitArguments("remote", "get-url", "origin"), Environment.CurrentDirectory)).Trim();
                var match = RemoteRegex().Match(remote);
                Require(match.Success, "Ambiguous or unavailable checkout repository; specify --repo OWNER/REPO.");
                repo = match.Groups[1].Value;
            }
            var output = parsed.Output.Length == 0
                ? Path.Combine(Path.GetTempPath(), $"review-bundle-{Guid.NewGuid()}")
                : parsed.Output;
            var result = await PrepareAsync(parsed with { Repo = repo, Output = output }, dependencies);
            var response = new JsonObject
            {
                ["manifest"] = Path.Combine(Path.GetFullPath(output), "manifest.json"),
                ["target"] = result["target"]!.DeepClone(),
                ["ready"] = true,
            };
            Console.Out.WriteLine(SerializeJson(response, indented: false));
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"BLOCKED: {error.Message}");
            return 1;
        }
    }

    private static readonly JsonSerializerOptions NodeValueJsonOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    private static Options ParseArguments(string[] args)
    {
        string? repo = null, pr = null, output = null, head = null, hostname = null, guidance = null, guidanceRoot = null;
        var check = false;
        for (var index = 0; index < args.Length; index++)
        {
            var name = args[index];
            if (name == "--check")
            {
                check = true;
                continue;
            }
            Require(name is "--repo" or "--pr" or "--output" or "--head" or "--hostname" or "--guidance" or "--guidance-root"
                && index + 1 < args.Length, $"Unknown or incomplete option: {name}");
            var value = args[++index];
            switch (name)
            {
                case "--repo": repo = value; break;
                case "--pr": pr = value; break;
                case "--output": output = value; break;
                case "--head": head = value; break;
                case "--hostname": hostname = value; break;
                case "--guidance": guidance = value; break;
                case "--guidance-root": guidanceRoot = value; break;
            }
        }
        return new(repo, pr, output ?? string.Empty, head, hostname, guidance, guidanceRoot, check);
    }

    internal static string[] GitArguments(params string[] args) => ["-c", "core.longpaths=true", .. args];

    private static byte[] Git(string directory, params string[] args) =>
        Run("git", GitArguments(["-C", directory, .. args]));

    private static byte[] Objects(string directory, params string[] args) =>
        Run("git", GitArguments(["--git-dir", directory, .. args]));

    internal static byte[] Run(string command, IReadOnlyList<string> args, string? workingDirectory = null, byte[]? input = null,
        int maximumProcessOutputBytes = MaximumProcessOutputBytes)
    {
        Require(maximumProcessOutputBytes > 0, "The maximum process output must be positive.");
        var start = new ProcessStartInfo(command)
        {
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardInput = input is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Cannot start {command}.");
        if (input is not null)
        {
            process.StandardInput.BaseStream.Write(input);
            process.StandardInput.Close();
        }
        var exceededOutputLimit = 0;
        async Task<byte[]> ReadOutputAsync()
        {
            using var output = new MemoryStream();
            var buffer = ArrayPool<byte>.Shared.Rent(81920);
            try
            {
                int read;
                while ((read = await process.StandardOutput.BaseStream.ReadAsync(buffer)) > 0)
                {
                    if (Volatile.Read(ref exceededOutputLimit) != 0
                        || output.Length > maximumProcessOutputBytes - read)
                    {
                        KillProcess();
                        continue;
                    }
                    await output.WriteAsync(buffer.AsMemory(0, read));
                }
                return output.ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        async Task<string> ReadErrorAsync()
        {
            var error = new StringBuilder();
            var buffer = ArrayPool<char>.Shared.Rent(4096);
            var bytes = 0;
            try
            {
                int read;
                while ((read = await process.StandardError.ReadAsync(buffer)) > 0)
                {
                    var additionalBytes = Utf8NoBom.GetByteCount(buffer, 0, read);
                    if (Volatile.Read(ref exceededOutputLimit) != 0
                        || bytes > maximumProcessOutputBytes - additionalBytes)
                    {
                        KillProcess();
                        continue;
                    }
                    error.Append(buffer, 0, read);
                    bytes += additionalBytes;
                }
                return error.ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }
        void KillProcess()
        {
            if (Interlocked.Exchange(ref exceededOutputLimit, 1) != 0)
            {
                return;
            }
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) when (process.HasExited)
            {
            }
        }
        var outputTask = ReadOutputAsync();
        var errorTask = ReadErrorAsync();
        Task.WaitAll(outputTask, errorTask);
        process.WaitForExit();
        Require(exceededOutputLimit == 0,
            $"{command} failed: process output exceeded 64 MiB.");
        if (process.ExitCode != 0)
        {
            var stderr = errorTask.Result;
            if (stderr.Length > 0)
            {
                Console.Error.Write(stderr);
            }
            var error = stderr.Trim();
            throw new InvalidOperationException($"{command} failed: {(error.Length > 0 ? error : $"exit code {process.ExitCode}")}");
        }
        return outputTask.Result;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static string Hash(byte[] bytes, HashAlgorithmName? algorithm = null)
    {
        var hash = algorithm == HashAlgorithmName.SHA1 ? SHA1.HashData(bytes) : SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    private static string BlobHash(byte[] bytes)
    {
        var prefix = Encoding.ASCII.GetBytes($"blob {bytes.Length}\0");
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        digest.AppendData(prefix);
        digest.AppendData(bytes);
        return Convert.ToHexStringLower(digest.GetHashAndReset());
    }

    private static string ProducerHash(string? sourcePath)
    {
        sourcePath ??= (string?)AppContext.GetData("EntryPointFilePath");
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new InvalidOperationException("Cannot identify the running prepare-review.cs source file.");
        }
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(sourcePath);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException($"Invalid running producer source path: {sourcePath}", error);
        }
        Require(Path.GetFileName(fullPath) == "prepare-review.cs",
            $"Running producer source is not the expected prepare-review.cs file: {fullPath}");
        Require(File.Exists(fullPath), $"Running producer source does not exist: {fullPath}");
        try
        {
            return Hash(File.ReadAllBytes(fullPath));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Cannot read running producer source: {fullPath}", error);
        }
    }

    internal static void CheckPaths(IEnumerable<string> names)
    {
        var files = new HashSet<string>(StringComparer.Ordinal);
        var directories = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            var parts = name.Split('/');
            Require(parts.All(part => part.Length > 0 && part is not "." and not ".."
                && !InvalidPathPartRegex().IsMatch(part) && !InvalidPathEndRegex().IsMatch(part)
                && !ReservedWindowsNameRegex().IsMatch(part)), $"Cannot export this path portably: {name}");
            var output = (name + Suffix).Normalize(NormalizationForm.FormC).ToLowerInvariant();
            Require(!files.Contains(output) && !directories.Contains(output), $"Export path collision: {name}");
            files.Add(output);
            var parents = output.Split('/').ToList();
            parents.RemoveAt(parents.Count - 1);
            while (parents.Count > 0)
            {
                var parent = string.Join('/', parents);
                Require(!files.Contains(parent), $"Export file/directory collision: {name}");
                directories.Add(parent);
                parents.RemoveAt(parents.Count - 1);
            }
        }
    }

    private static async Task WriteAsync(string directory, string name, byte[] bytes)
    {
        var destination = Path.Combine(directory, name.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
        };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }
        await using var stream = new FileStream(destination, options);
        await stream.WriteAsync(bytes);
    }

    private static Task WriteAsync(string directory, string name, string text) =>
        WriteAsync(directory, name, Utf8NoBom.GetBytes(text));

    private static void CreateNewDirectory(string path)
    {
        Require(!Directory.Exists(path) && !File.Exists(path), $"Cannot create directory because it already exists: {path}");
        Directory.CreateDirectory(path);
    }

    private static void CreateOutputDirectory(string path)
    {
        Require(!Directory.Exists(path) && !File.Exists(path), $"Output directory already exists: {path}");
        Directory.CreateDirectory(path);
    }

    private static void DeleteOutputDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
        foreach (var name in Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(name, File.GetAttributes(name) & ~FileAttributes.ReadOnly);
        }
        File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
        Directory.Delete(path, recursive: true);
    }

    private static List<string> Walk(string directory, string prefix = "")
    {
        var result = new List<string>();
        var current = Path.Combine(directory, prefix.Replace('/', Path.DirectorySeparatorChar));
        foreach (var entry in Directory.EnumerateFileSystemEntries(current))
        {
            var info = new FileInfo(entry);
            var name = prefix.Length > 0 ? $"{prefix}/{Path.GetFileName(entry)}" : Path.GetFileName(entry);
            Require(info.LinkTarget is null, $"Prepared input became a symlink: {name}");
            if ((info.Attributes & FileAttributes.Directory) != 0)
            {
                result.AddRange(Walk(directory, name));
            }
            else
            {
                Require((info.Attributes & (FileAttributes.Device | FileAttributes.ReparsePoint)) == 0,
                    $"Prepared input is not an ordinary file: {name}");
                result.Add(name);
            }
        }
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static Task<JsonObject> DirectoryDigestAsync(string directory)
    {
        var names = Walk(directory);
        var hashes = new string[names.Count];
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount * 2, 4, 16),
        };
        return CompleteAsync();

        async Task<JsonObject> CompleteAsync()
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, names.Count), parallelOptions, async (index, cancellationToken) =>
            {
                var path = Path.Combine(directory, names[index].Replace('/', Path.DirectorySeparatorChar));
                await using var stream = new FileStream(path, new FileStreamOptions
                {
                    Mode = FileMode.Open,
                    Access = FileAccess.Read,
                    Share = FileShare.Read,
                    BufferSize = 0,
                    Options = FileOptions.Asynchronous | FileOptions.RandomAccess,
                });
                hashes[index] = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
            });
            using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            for (var index = 0; index < names.Count; index++)
            {
                digest.AppendData(Utf8NoBom.GetBytes($"{names[index]}\0{hashes[index]}\n"));
            }
            return new JsonObject
            {
                ["files"] = names.Count,
                ["sha256"] = Convert.ToHexStringLower(digest.GetHashAndReset()),
            };
        }
    }

    private static List<TreeEntry> TreeEntries(string store, string commit)
    {
        var result = new List<TreeEntry>();
        foreach (var line in Utf8NoBom.GetString(Objects(store, "ls-tree", "-r", "-z", "--full-tree", commit)).Split('\0',
            StringSplitOptions.RemoveEmptyEntries))
        {
            var tab = line.IndexOf('\t');
            var fields = tab > 0 ? line[..tab].Split(' ') : [];
            Require(tab > 0 && fields.Length == 3 && FullShaRegex().IsMatch(fields[2])
                && fields[1] is "blob" or "commit", "Malformed Git tree entry.");
            result.Add(new(fields[0], fields[1], fields[2], line[(tab + 1)..]));
        }
        return result;
    }

    private static List<(string Name, string Body)> Sections(string markdown, int level)
    {
        var matches = Regex.Matches(markdown, $"^{"#".Repeat(level)} (.+)$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);
        var result = new List<(string, string)>();
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var end = index + 1 < matches.Count ? matches[index + 1].Index : markdown.Length;
            result.Add((match.Groups[1].Value.Trim(), markdown[match.Index..end].Trim()));
        }
        return result;
    }

    private static string Repeat(this string value, int count) => string.Concat(Enumerable.Repeat(value, count));

    internal static GuideResult ValidateGuide(string markdown, string name)
    {
        var groups = Sections(markdown, 2);
        foreach (var heading in new[] { "Overarching principles", "Topics" })
        {
            Require(groups.Count(group => group.Name == heading) == 1,
                $"Required guide {name} needs exactly one ## {heading} section.");
        }
        var principles = groups.Single(group => group.Name == "Overarching principles").Body;
        var topics = groups.Single(group => group.Name == "Topics").Body;
        Require(BulletRegex().IsMatch(principles), $"Required guide {name} has empty overarching principles.");
        var entries = Sections(topics, 3);
        Require(entries.Count > 0 && entries.Select(entry => entry.Name).Distinct(StringComparer.Ordinal).Count() == entries.Count
            && entries.All(entry => entry.Name.Length > 0 && BulletRegex().IsMatch(entry.Body)),
            $"Required guide {name} has missing, duplicate, or empty topics.");
        return new(principles, entries.Select(entry => entry.Name).ToList(), topics);
    }

    private static string AnchorFor(string title) => NonAnchorRegex().Replace(TagRegex().Replace(title.ToLowerInvariant(), ""), "")
        .Replace(" ", "-", StringComparison.Ordinal);

    internal static List<RoutingEntry> ParseRouting(string markdown)
    {
        var lines = markdown.ReplaceLineEndings("\n").Split('\n').Select(line => line.Trim()).ToArray();
        const string header = "| Changed path prefix | Guide |";
        var headers = lines.Select((line, index) => (line, index)).Where(item => item.line == header).ToList();
        Require(headers.Count == 1, "Required routing table is empty or malformed.");
        var headerIndex = headers[0].index;
        Require(headerIndex + 2 < lines.Length && lines[headerIndex + 1] == "| --- | --- |",
            "Required routing table is empty or malformed.");
        var entries = new List<RoutingEntry>();
        var tableLines = new HashSet<int> { headerIndex, headerIndex + 1 };
        for (var index = headerIndex + 2; index < lines.Length && lines[index].StartsWith('|'); index++)
        {
            tableLines.Add(index);
            var cells = lines[index].Split('|');
            Require(cells.Length == 4 && cells[0].Length == 0 && cells[3].Length == 0,
                "Required routing table is empty or malformed.");
            var prefix = cells[1].Trim();
            var guide = cells[2].Trim();
            Require(prefix.Length > 0 && guide.EndsWith(".md", StringComparison.Ordinal),
                "Required routing table is empty or malformed.");
            if (prefix == "*")
            {
                CheckPaths([guide]);
            }
            else
            {
                Require(prefix.EndsWith('/') && !prefix.Contains('*', StringComparison.Ordinal),
                    "Every routing prefix except * must end in /.");
                CheckPaths([prefix[..^1], guide]);
            }
            entries.Add(new(prefix, guide));
        }
        Require(entries.Count > 0 && lines.Select((line, index) => (line, index))
            .Where(item => item.line.StartsWith('|')).All(item => tableLines.Contains(item.index)),
            "Required routing table is empty or malformed.");
        Require(entries.Any(entry => entry.Prefix == "*"), "Required routing table needs a * row.");
        Require(entries.Distinct().Count() == entries.Count, "Required routing table has a duplicate row.");
        return entries;
    }

    private static async Task<RoutingResult> RouteGuidesAsync(string guidanceRoot, JsonArray files)
    {
        var routingFile = Path.Combine(guidanceRoot, (RoutingPath + Suffix).Replace('/', Path.DirectorySeparatorChar));
        Require(File.Exists(routingFile), $"Required routing table is missing: {RoutingPath}");
        var routingBytes = await File.ReadAllBytesAsync(routingFile);
        var entries = ParseRouting(Utf8NoBom.GetString(routingBytes));
        foreach (var guide in entries.Select(entry => entry.Guide).Distinct(StringComparer.Ordinal))
        {
            var guideFile = Path.Combine(guidanceRoot, (guide + Suffix).Replace('/', Path.DirectorySeparatorChar));
            Require(File.Exists(guideFile), $"Required guide {guide} is missing.");
            ValidateGuide(await File.ReadAllTextAsync(guideFile, Utf8NoBom), guide);
        }
        var guides = new List<string>();
        foreach (var entry in entries)
        {
            if ((entry.Prefix == "*" || files.Any(file =>
                    new[] { file?["filename"]?.GetValue<string>(), file?["previous_filename"]?.GetValue<string>() }
                        .Any(name => name is not null && name.StartsWith(entry.Prefix, StringComparison.Ordinal))))
                && !guides.Contains(entry.Guide, StringComparer.Ordinal))
            {
                guides.Add(entry.Guide);
            }
        }
        return new(Hash(routingBytes), guides);
    }

    internal static GuideLinkResult GuideLinks(string text, string guidePath)
    {
        var included = new List<Link>();
        var context = new List<Link>();
        var skipped = new List<Link>();
        foreach (var line in text.Split('\n'))
        {
            foreach (Match match in MarkdownLinkRegex().Matches(line))
            {
                var destination = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                if (!MarkdownDestinationRegex().IsMatch(destination))
                {
                    continue;
                }
                var hash = destination.IndexOf('#');
                var relative = hash >= 0 ? destination[..hash] : destination;
                var anchor = hash >= 0 ? destination[(hash + 1)..] : null;
                if (SchemeRegex().IsMatch(relative) || relative.StartsWith('/'))
                {
                    continue;
                }
                var resolved = NormalizePosix(PathJoinPosix(PosixDirectoryName(guidePath), relative));
                CheckPaths([resolved]);
                Require(anchor is null || ValidAnchorRegex().IsMatch(anchor), $"Invalid required policy anchor: {destination}");
                var link = new Link(resolved, string.IsNullOrEmpty(anchor) ? null : anchor, guidePath);
                if (SkippedLineRegex().IsMatch(line))
                {
                    skipped.Add(link with { Reason = "Supporting source example, not a delegated criterion." });
                }
                else if (anchor is not null)
                {
                    included.Add(link);
                }
                else
                {
                    context.Add(link with { Role = "context" });
                }
            }
        }
        return new(included, context, skipped);
    }

    private static string PosixDirectoryName(string path)
    {
        var index = path.LastIndexOf('/');
        return index < 0 ? "." : path[..index];
    }

    private static string PathJoinPosix(string left, string right) => $"{left}/{right}";

    private static string NormalizePosix(string path)
    {
        var stack = new List<string>();
        foreach (var part in path.Split('/'))
        {
            if (part is "" or ".")
            {
                continue;
            }
            if (part == "..")
            {
                if (stack.Count > 0 && stack[^1] != "..")
                {
                    stack.RemoveAt(stack.Count - 1);
                }
                else
                {
                    stack.Add(part);
                }
            }
            else
            {
                stack.Add(part);
            }
        }
        return string.Join('/', stack);
    }

    private static JsonObject LinkJson(Link link)
    {
        var json = new JsonObject { ["path"] = link.Path };
        if (link.Anchor is not null) json["anchor"] = link.Anchor;
        json["guide"] = link.Guide;
        if (link.Role is not null) json["role"] = link.Role;
        if (link.Reason is not null) json["reason"] = link.Reason;
        return json;
    }

    private static async Task<JsonArray> ContextLinksAsync(IEnumerable<Link> links, string guidanceRoot, JsonArray? pointers = null)
    {
        var context = new JsonArray();
        foreach (var link in links)
        {
            var pointer = pointers?.FirstOrDefault(item => item?["path"]?.GetValue<string>() == link.Path);
            var item = LinkJson(link);
            if (pointer is not null)
            {
                item["sha256"] = null;
                item["status"] = "unreadable";
                item["reason"] = pointer["kind"]!.GetValue<string>();
                context.Add(item);
                continue;
            }
            try
            {
                var bytes = await File.ReadAllBytesAsync(Path.Combine(guidanceRoot,
                    (link.Path + Suffix).Replace('/', Path.DirectorySeparatorChar)));
                item["sha256"] = Hash(bytes);
                item["status"] = "readable";
            }
            catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException
                or UnauthorizedAccessException or IOException)
            {
                item["sha256"] = null;
                item["status"] = error is FileNotFoundException or DirectoryNotFoundException ? "missing" : "unreadable";
                item["reason"] = error is FileNotFoundException or DirectoryNotFoundException ? "ENOENT" : "EACCES";
            }
            context.Add(item);
        }
        return context;
    }

    internal static string ResolvePolicy(string markdown, string anchor, string name)
    {
        var headings = HeadingRegex().Matches(markdown).Cast<Match>().ToList();
        var matches = headings.Where(match => AnchorFor(match.Groups[2].Value) == anchor).ToList();
        Require(matches.Count == 1, $"Missing or ambiguous required policy {name}#{anchor}.");
        var start = matches[0];
        var end = headings.FirstOrDefault(match => match.Index > start.Index
            && match.Groups[1].Value.Length <= start.Groups[1].Value.Length);
        var body = markdown[start.Index..(end?.Index ?? markdown.Length)].Trim();
        Require(body.Length > start.Value.Length, $"Empty required policy {name}#{anchor}.");
        return body;
    }

    internal static async Task<JsonObject> ExportTreeAsync(
        string store, string commit, string destination, Func<string, bool>? selection = null)
    {
        selection ??= static _ => true;
        var entries = TreeEntries(store, commit).Where(entry => selection(entry.Name)).ToList();
        CheckPaths(entries.Select(entry => entry.Name));
        CreateNewDirectory(destination);
        var blobs = entries.Where(entry => entry.Type == "blob").ToList();
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in GitArguments("--git-dir", store, "cat-file", "--batch"))
        {
            start.ArgumentList.Add(arg);
        }
        using var child = Process.Start(start) ?? throw new InvalidOperationException("Cannot start git.");
        var inputTask = WriteBlobRequestsAsync(child, blobs);
        var stderrTask = child.StandardError.ReadToEndAsync();
        using var stdout = new BufferedByteReader(child.StandardOutput.BaseStream, 1024 * 1024);
        var createdDirectories = new HashSet<string>(StringComparer.Ordinal);
        var exportedHashes = new List<(string Name, string Sha256)>(entries.Count);
        var pointers = new JsonArray();
        try
        {
            foreach (var entry in blobs)
            {
                var header = await stdout.ReadAsciiLineAsync();
                var fields = header.Split(' ');
                var size = -1;
                Require(fields.Length == 3 && fields[0] == entry.Sha && fields[1] == "blob"
                    && int.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out size),
                    "Unexpected Git blob response.");
                Require(size <= MaximumBlobBytes, $"Source blob exceeds 16 MiB: {entry.Name}");
                var body = new byte[size];
                await stdout.ReadExactlyAsync(body);
                Require(await stdout.ReadByteAsync() == 10, $"Blob mismatch: {entry.Name}");
                Require(BlobHash(body) == entry.Sha, $"Blob mismatch: {entry.Name}");
                exportedHashes.Add((entry.Name + Suffix, Hash(body)));
                WriteExportFile(destination, entry.Name + Suffix, body, createdDirectories);
                var lfsPrefix = Utf8NoBom.GetBytes("version https://git-lfs.github.com/spec/v1");
                var lfs = body.Length >= lfsPrefix.Length && body.AsSpan(0, lfsPrefix.Length).SequenceEqual(lfsPrefix);
                if (entry.Mode == "120000" || lfs)
                {
                    pointers.Add(new JsonObject
                    {
                        ["path"] = entry.Name,
                        ["kind"] = entry.Mode == "120000" ? "symlink-text" : "lfs-pointer",
                    });
                }
            }
            await inputTask;
            await child.WaitForExitAsync();
            var stderr = await stderrTask;
            Require(child.ExitCode == 0, $"Git blob export failed: {stderr}");
        }
        catch
        {
            if (!child.HasExited)
            {
                child.Kill(entireProcessTree: true);
            }
            throw;
        }
        foreach (var entry in entries.Where(entry => entry.Type == "commit"))
        {
            var body = Utf8NoBom.GetBytes($"Unmaterialized submodule commit: {entry.Sha}\n");
            WriteExportFile(destination, entry.Name + Suffix, body, createdDirectories);
            exportedHashes.Add((entry.Name + Suffix, Hash(body)));
            pointers.Add(new JsonObject { ["path"] = entry.Name, ["kind"] = "submodule", ["commit"] = entry.Sha });
        }
        var result = DirectoryDigest(exportedHashes);
        result["pointers"] = pointers;
        return result;
    }

    private static JsonObject DirectoryDigest(IEnumerable<(string Name, string Sha256)> files)
    {
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var count = 0;
        foreach (var file in files.OrderBy(file => file.Name, StringComparer.Ordinal))
        {
            digest.AppendData(Utf8NoBom.GetBytes($"{file.Name}\0{file.Sha256}\n"));
            count++;
        }
        return new JsonObject
        {
            ["files"] = count,
            ["sha256"] = Convert.ToHexStringLower(digest.GetHashAndReset()),
        };
    }

    private static async Task WriteBlobRequestsAsync(Process child, IEnumerable<TreeEntry> blobs)
    {
        try
        {
            var requests = string.Join('\n', blobs.Select(entry => entry.Sha));
            if (requests.Length > 0)
            {
                await child.StandardInput.WriteAsync(requests);
                await child.StandardInput.WriteLineAsync();
            }
        }
        finally
        {
            child.StandardInput.Close();
        }
    }

    private static void WriteExportFile(
        string directory, string name, byte[] bytes, HashSet<string> createdDirectories)
    {
        var destination = Path.Combine(directory, name.Replace('/', Path.DirectorySeparatorChar));
        var parent = Path.GetDirectoryName(destination)!;
        if (createdDirectories.Add(parent))
        {
            Directory.CreateDirectory(parent);
        }
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = 0,
        };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }
        using var stream = new FileStream(destination, options);
        stream.Write(bytes);
    }

    private sealed class BufferedByteReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly byte[] _buffer;
        private int _offset;
        private int _count;

        public BufferedByteReader(Stream stream, int bufferSize)
        {
            _stream = stream;
            _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        }

        public async ValueTask<string> ReadAsciiLineAsync()
        {
            ArrayBufferWriter<byte>? overflow = null;
            while (true)
            {
                if (_offset == _count)
                {
                    Require(await FillAsync(), "Incomplete Git blob stream.");
                }
                var newline = Array.IndexOf(_buffer, (byte)'\n', _offset, _count - _offset);
                if (newline >= 0)
                {
                    var segment = _buffer.AsSpan(_offset, newline - _offset);
                    _offset = newline + 1;
                    if (overflow is null)
                    {
                        return Encoding.ASCII.GetString(segment);
                    }
                    overflow.Write(segment);
                    return Encoding.ASCII.GetString(overflow.WrittenSpan);
                }
                overflow ??= new ArrayBufferWriter<byte>();
                overflow.Write(_buffer.AsSpan(_offset, _count - _offset));
                _offset = _count;
            }
        }

        public async ValueTask ReadExactlyAsync(Memory<byte> destination)
        {
            while (destination.Length > 0)
            {
                if (_offset < _count)
                {
                    var length = Math.Min(destination.Length, _count - _offset);
                    _buffer.AsMemory(_offset, length).CopyTo(destination);
                    _offset += length;
                    destination = destination[length..];
                }
                else if (destination.Length >= _buffer.Length)
                {
                    var read = await _stream.ReadAsync(destination);
                    Require(read > 0, "Incomplete Git blob stream.");
                    destination = destination[read..];
                }
                else
                {
                    Require(await FillAsync(), "Incomplete Git blob stream.");
                }
            }
        }

        public async ValueTask<byte> ReadByteAsync()
        {
            if (_offset == _count)
            {
                Require(await FillAsync(), "Incomplete Git blob stream.");
            }
            return _buffer[_offset++];
        }

        private async ValueTask<bool> FillAsync()
        {
            _offset = 0;
            _count = await _stream.ReadAsync(_buffer);
            return _count > 0;
        }

        public void Dispose() => ArrayPool<byte>.Shared.Return(_buffer);
    }

    private static async Task<JsonObject> LocalGuidanceAsync(string root)
    {
        root = Utf8NoBom.GetString(Git(root, "rev-parse", "--show-toplevel")).Trim();
        var names = Utf8NoBom.GetString(Git(root, "ls-files", "-z", "--cached", "--others", "--exclude-standard",
            "--", "*.md", "AGENTS.md")).Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var files = new JsonArray();
        foreach (var name in names)
        {
            var path = Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar));
            if (!Path.Exists(path))
            {
                continue;
            }
            var info = new FileInfo(path);
            Require(info.LinkTarget is null && (info.Attributes & (FileAttributes.Directory | FileAttributes.Device | FileAttributes.ReparsePoint)) == 0,
                $"Guidance must be an ordinary file: {name}");
            files.Add(new JsonObject { ["name"] = name, ["body"] = Convert.ToBase64String(await File.ReadAllBytesAsync(path)) });
        }
        CheckPaths(files.Select(file => file!["name"]!.GetValue<string>()));
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var name in files.Select(file => file!["name"]!.GetValue<string>() + Suffix).Order(StringComparer.Ordinal))
        {
            var file = files.Single(file => file!["name"]!.GetValue<string>() + Suffix == name)!;
            var body = Convert.FromBase64String(file["body"]!.GetValue<string>());
            digest.AppendData(Utf8NoBom.GetBytes($"{name}\0{Hash(body)}\n"));
        }
        return new JsonObject
        {
            ["mode"] = "local",
            ["originalRoot"] = root,
            ["checkoutCommit"] = Utf8NoBom.GetString(Git(root, "rev-parse", "HEAD")).Trim(),
            ["workingTreeChanges"] = Git(root, "status", "--porcelain", "--untracked-files=all",
                "--", "*.md", "AGENTS.md").Length > 0,
            ["sha256"] = Convert.ToHexStringLower(digest.GetHashAndReset()),
            ["selectedFiles"] = files,
        };
    }

    internal static async Task<JsonObject> PrepareAsync(Options options, Dependencies? dependencies = null)
    {
        try
        {
            return await PrepareOnceAsync(options, dependencies);
        }
        catch (InvalidOperationException error) when (!options.Check && error.Message == TargetMovedDuringPreparationMessage)
        {
            DeleteOutputDirectory(Path.GetFullPath(options.Output));
            return await PrepareOnceAsync(options, dependencies);
        }
    }

    private static async Task<JsonObject> PrepareOnceAsync(Options options, Dependencies? dependencies)
    {
        dependencies ??= new();
        var timings = new JsonObject();
        async Task<T> Timed<T>(string name, Func<Task<T>> action)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return await action();
            }
            finally
            {
                timings[name] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 3);
            }
        }

        Require(RepositoryNameRegex().IsMatch(options.Repo ?? "") && PositiveIntegerRegex().IsMatch(options.Pr ?? ""),
            "Cannot resolve a single target repository; specify --repo OWNER/REPO and --pr NUMBER.");
        Require(options.Output.Length > 0, "Specify a new --output directory, or --check an existing prepared directory.");
        Require(options.Head is null || FullShaRegex().IsMatch(options.Head), "--head must be a full immutable commit.");
        Require(options.Guidance is null || options.GuidanceRoot is null, "Select either --guidance or --guidance-root.");
        var producer = ProducerHash(dependencies.ProducerSourcePath);
        var host = options.Hostname ?? "github.com";
        Require(HostnameRegex().IsMatch(host), "Invalid GitHub hostname.");
        Run("git", GitArguments("--version"));
        Run("gh", ["--version"]);

        async Task<JsonNode?> Api(string endpoint, string? accept = null)
        {
            if (dependencies.Api is not null)
            {
                return await dependencies.Api(endpoint, accept);
            }
            var arguments = new List<string> { "api", "--hostname", host, endpoint };
            if (accept is not null)
            {
                arguments.Add("-H");
                arguments.Add($"Accept: {accept}");
                return JsonValue.Create(Convert.ToBase64String(Run("gh", arguments)));
            }
            return JsonNode.Parse(Run("gh", arguments));
        }

        JsonObject repository = null!;
        string endpoint = null!;
        async Task<(JsonObject Identity, JsonObject Pull)> Freeze()
        {
            var pull = (await Api($"{endpoint}/pulls/{options.Pr}"))!.AsObject();
            Require(pull["number"]!.GetValue<int>() == int.Parse(options.Pr!, CultureInfo.InvariantCulture)
                && pull["state"]!.GetValue<string>() == "open"
                && JsonInteger(pull["base"]!["repo"]!["id"]!) == JsonInteger(repository["id"]!)
                && RepositoryNameRegex().IsMatch(pull["head"]!["repo"]!["full_name"]?.GetValue<string>() ?? "")
                && FullShaRegex().IsMatch(pull["head"]!["sha"]!.GetValue<string>()),
                "GitHub did not identify the requested PR and its head repository.");
            var head = pull["head"]!["sha"]!.GetValue<string>();
            Require(options.Head is null || options.Head == head, "The live PR head differs from the expected frozen head.");
            var baseRef = pull["base"]!["ref"]!.GetValue<string>();
            var encodedRef = Uri.EscapeDataString(baseRef);
            var branch = (await Api($"{endpoint}/git/ref/heads/{encodedRef}"))!.AsObject();
            var baseTip = branch["object"]?["sha"]?.GetValue<string>() ?? "";
            Require(FullShaRegex().IsMatch(baseTip), "The base branch did not resolve to a full commit.");
            var comparison = (await Api($"{endpoint}/compare/{baseTip}...{head}"))!.AsObject();
            var mergeBase = comparison["merge_base_commit"]?["sha"]?.GetValue<string>() ?? "";
            Require(comparison["base_commit"]?["sha"]?.GetValue<string>() == baseTip && FullShaRegex().IsMatch(mergeBase),
                "GitHub did not return the expected immutable comparison identities.");
            return (new JsonObject
            {
                ["hostname"] = host,
                ["repository"] = repository["full_name"]!.GetValue<string>(),
                ["repositoryId"] = JsonInteger(repository["id"]!),
                ["pr"] = pull["number"]!.GetValue<int>(),
                ["headRepository"] = pull["head"]!["repo"]!["full_name"]!.GetValue<string>(),
                ["head"] = head,
                ["baseRepository"] = pull["base"]!["repo"]!["full_name"]!.GetValue<string>(),
                ["baseRef"] = baseRef,
                ["baseTip"] = baseTip,
                ["mergeBase"] = mergeBase,
            }, pull);
        }

        var frozen = await Timed("apiFreeze", async () =>
        {
            repository = (await Api($"repos/{options.Repo}"))!.AsObject();
            Require(RepositoryNameRegex().IsMatch(repository["full_name"]?.GetValue<string>() ?? "")
                && TryJsonSafeInteger(repository["id"], out _), "Invalid target repository metadata.");
            endpoint = $"repos/{repository["full_name"]!.GetValue<string>()}";
            return await Freeze();
        });
        var output = Path.GetFullPath(options.Output);
        JsonObject guidance;
        if (options.Guidance is not null)
        {
            var parts = options.Guidance.Split('@');
            Require(parts.Length == 2 && RepositoryNameRegex().IsMatch(parts[0]) && FullShaRegex().IsMatch(parts[1]),
                "--guidance requires OWNER/REPO@FULL_COMMIT.");
            var selected = (await Api($"repos/{parts[0]}"))!.AsObject();
            Require(RepositoryNameRegex().IsMatch(selected["full_name"]?.GetValue<string>() ?? ""), "Invalid guidance repository.");
            guidance = new JsonObject { ["mode"] = "remote", ["repository"] = selected["full_name"]!.GetValue<string>(), ["commit"] = parts[1] };
        }
        else
        {
            guidance = await LocalGuidanceAsync(options.GuidanceRoot ?? Environment.CurrentDirectory);
        }

        if (options.Check)
        {
            return await CheckPreparedAsync(output, producer, frozen.Identity, guidance, Freeze);
        }

        CreateOutputDirectory(output);
        var store = Path.Combine(output, ".objects");
        Run("git", GitArguments("init", "--bare", "--quiet", "--object-format=sha1", store));
        Objects(store, "config", "core.hooksPath", Path.Combine(store, "disabled-hooks"));
        async Task Fetch(string repo, IReadOnlyList<string> commits)
        {
            if (dependencies.Fetch is not null)
            {
                await dependencies.Fetch(repo, commits, store);
            }
            else
            {
                Objects(store, ["-c", "credential.helper=", "-c", "credential.helper=!gh auth git-credential",
                    "fetch", "--quiet", "--no-tags", "--depth=1", $"https://{host}/{repo}.git", .. commits]);
            }
        }
        var groups = new List<(string Repository, List<string> Commits)>();
        var groupIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in new[]
        {
            (frozen.Identity["headRepository"]!.GetValue<string>(), frozen.Identity["head"]!.GetValue<string>()),
            (frozen.Identity["baseRepository"]!.GetValue<string>(), frozen.Identity["baseTip"]!.GetValue<string>()),
            (frozen.Identity["baseRepository"]!.GetValue<string>(), frozen.Identity["mergeBase"]!.GetValue<string>()),
        }.Concat(guidance["mode"]!.GetValue<string>() == "remote"
            ? [(guidance["repository"]!.GetValue<string>(), guidance["commit"]!.GetValue<string>())] : []))
        {
            if (!groupIndexes.TryGetValue(pair.Item1, out var groupIndex))
            {
                groupIndex = groups.Count;
                groupIndexes.Add(pair.Item1, groupIndex);
                groups.Add((pair.Item1, []));
            }
            var commits = groups[groupIndex].Commits;
            if (!commits.Contains(pair.Item2, StringComparer.Ordinal))
            {
                commits.Add(pair.Item2);
            }
        }
        await Timed("fetch", async () =>
        {
            foreach (var pair in groups)
            {
                await Fetch(pair.Repository, pair.Commits);
            }
            return true;
        });

        var files = await Timed("changedFiles", async () =>
        {
            var result = new JsonArray();
            for (var page = 1; ; page++)
            {
                var batch = (await Api($"{endpoint}/pulls/{options.Pr}/files?per_page=100&page={page}"))?.AsArray()
                    ?? throw new InvalidOperationException("GitHub returned an invalid file list.");
                foreach (var item in batch)
                {
                    result.Add(item!.DeepClone());
                }
                if (batch.Count < 100)
                {
                    return result;
                }
            }
        });
        Require(files.Count == frozen.Pull["changed_files"]!.GetValue<int>()
            && files.Select(file => file!["filename"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count() == files.Count,
            "GitHub returned an incomplete or duplicate changed-file list.");
        var changedPaths = Utf8NoBom.GetString(Objects(store, "diff", "--no-ext-diff", "--no-textconv", "--no-renames",
            "--name-only", "-z", frozen.Identity["mergeBase"]!.GetValue<string>(), frozen.Identity["head"]!.GetValue<string>()))
            .Split('\0', StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal).ToArray();
        var listedPaths = files.SelectMany(file => new[]
            {
                file!["filename"]!.GetValue<string>(),
                file["previous_filename"]?.GetValue<string>(),
            }).Where(name => name is not null).Cast<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        Require(changedPaths.SequenceEqual(listedPaths, StringComparer.Ordinal), "GitHub file list does not match the frozen trees.");
        foreach (var file in files)
        {
            var name = file!["filename"]!.GetValue<string>();
            var side = file["status"]!.GetValue<string>() == "removed"
                ? frozen.Identity["mergeBase"]!.GetValue<string>() : frozen.Identity["head"]!.GetValue<string>();
            Require(Utf8NoBom.GetString(Objects(store, "rev-parse", $"{side}:{name}")).Trim() == file["sha"]!.GetValue<string>(),
                $"GitHub file identity does not match the frozen tree: {name}");
        }

        await Timed("diff", async () =>
        {
            var node = await Api($"{endpoint}/pulls/{options.Pr}", "application/vnd.github.diff");
            Require(node is JsonValue, "GitHub did not return the authoritative diff bytes.");
            var diff = Convert.FromBase64String(node!.GetValue<string>());
            await WriteAsync(output, "diff.patch", diff);
            Objects(store, "read-tree", frozen.Identity["mergeBase"]!.GetValue<string>());
            if (diff.Length > 0)
            {
                Objects(store, "apply", "--cached", "--binary", "--whitespace=nowarn", Path.Combine(output, "diff.patch"));
            }
            Require(Utf8NoBom.GetString(Objects(store, "write-tree")).Trim()
                == Utf8NoBom.GetString(Objects(store, "rev-parse", $"{frozen.Identity["head"]!.GetValue<string>()}^{{tree}}")).Trim(),
                "The authoritative diff does not reconstruct the frozen head; incomplete or unsupported diff.");
            return true;
        });
        await WriteJsonAsync(output, "files.json", files);
        await WriteJsonAsync(output, "pull.json", frozen.Pull);

        async Task<JsonArray> Paginate(string uri)
        {
            var items = new JsonArray();
            for (var page = 1; ; page++)
            {
                var batch = (await Api($"{endpoint}/{uri}?per_page=100&page={page}"))?.AsArray()
                    ?? throw new InvalidOperationException($"Invalid review feedback at {uri}.");
                foreach (var item in batch) items.Add(item!.DeepClone());
                if (batch.Count < 100) return items;
            }
        }
        await Timed("feedback", async () =>
        {
            var feedback = new JsonObject
            {
                ["comments"] = await Paginate($"issues/{options.Pr}/comments"),
                ["reviews"] = await Paginate($"pulls/{options.Pr}/reviews"),
                ["inline"] = await Paginate($"pulls/{options.Pr}/comments"),
            };
            await WriteJsonAsync(output, "feedback.json", feedback);
            return true;
        });

        Directory.CreateDirectory(Path.Combine(output, "source"));
        var sources = new JsonObject();
        var exported = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var role in new[] { "head", "mergeBase", "baseTip" })
        {
            var commit = frozen.Identity[role]!.GetValue<string>();
            if (!exported.TryGetValue(commit, out var source))
            {
                var root = $"source/{commit}";
                var treeExport = await Timed($"exportTree:{commit}",
                    () => ExportTreeAsync(store, commit, Path.Combine(output, root.Replace('/', Path.DirectorySeparatorChar))));
                source = new JsonObject
                {
                    ["root"] = root,
                    ["commit"] = commit,
                    ["tree"] = Utf8NoBom.GetString(Objects(store, "rev-parse", $"{commit}^{{tree}}")).Trim(),
                };
                foreach (var pair in treeExport) source[pair.Key] = pair.Value?.DeepClone();
                exported[commit] = source;
            }
            sources[role] = source.DeepClone();
        }
        if (guidance["mode"]!.GetValue<string>() == "remote")
        {
            var exportedGuidance = await Timed("exportGuidance", () => ExportTreeAsync(store,
                guidance["commit"]!.GetValue<string>(), Path.Combine(output, "guidance"), name => name.EndsWith(".md", StringComparison.Ordinal)));
            guidance["root"] = "guidance";
            foreach (var pair in exportedGuidance) guidance[pair.Key] = pair.Value?.DeepClone();
        }
        else
        {
            var selected = guidance["selectedFiles"]!.AsArray();
            guidance.Remove("selectedFiles");
            Directory.CreateDirectory(Path.Combine(output, "guidance"));
            foreach (var file in selected)
            {
                await WriteAsync(Path.Combine(output, "guidance"), file!["name"]!.GetValue<string>() + Suffix,
                    Convert.FromBase64String(file["body"]!.GetValue<string>()));
            }
            guidance["root"] = "guidance";
            var digest = await DirectoryDigestAsync(Path.Combine(output, "guidance"));
            foreach (var pair in digest) guidance[pair.Key] = pair.Value?.DeepClone();
            var current = await LocalGuidanceAsync(guidance["originalRoot"]!.GetValue<string>());
            Require(current["sha256"]!.GetValue<string>() == guidance["sha256"]!.GetValue<string>(),
                "Working-tree guidance changed during preparation.");
        }

        var guides = new JsonArray();
        var policies = new JsonArray();
        var context = new JsonArray();
        var routing = await RouteGuidesAsync(Path.Combine(output, "guidance"), files);
        var exclusions = new JsonArray
        {
            new JsonObject
            {
                ["scope"] = "Running PR code, tests, CI, browser workflows, or implementation samples",
                ["reason"] = "This is a source-only review; assess changed tests and contracts from source.",
            },
        };
        const string instructionPath = ".github/copilot-instructions.md";
        var instruction = await File.ReadAllTextAsync(Path.Combine(output, "guidance",
            (instructionPath + Suffix).Replace('/', Path.DirectorySeparatorChar)), Utf8NoBom);
        exclusions.Add(new JsonObject
        {
            ["source"] = $"{instructionPath}#security-concerns-are-out-of-scope",
            ["body"] = ResolvePolicy(instruction, "security-concerns-are-out-of-scope", instructionPath),
        });
        var skippedLinks = new JsonArray();
        foreach (var name in routing.Guides)
        {
            var body = await File.ReadAllTextAsync(Path.Combine(output, "guidance",
                (name + Suffix).Replace('/', Path.DirectorySeparatorChar)), Utf8NoBom);
            var parsed = ValidateGuide(body, name);
            guides.Add(new JsonObject { ["path"] = name, ["topics"] = new JsonArray(parsed.Topics.Select(topic => JsonValue.Create(topic)).ToArray()) });
            var links = GuideLinks(body, name);
            foreach (var link in links.Skipped) skippedLinks.Add(LinkJson(link));
            var classified = await ContextLinksAsync(links.Context, Path.Combine(output, "guidance"), guidance["pointers"]?.AsArray());
            foreach (var item in classified) context.Add(item!.DeepClone());
            foreach (var link in links.Included)
            {
                var target = await File.ReadAllTextAsync(Path.Combine(output, "guidance",
                    (link.Path + Suffix).Replace('/', Path.DirectorySeparatorChar)), Utf8NoBom);
                var item = LinkJson(link);
                item["body"] = ResolvePolicy(target, link.Anchor!, link.Path);
                policies.Add(item);
            }
        }
        Require(JsonEqual((await Freeze()).Identity, frozen.Identity), TargetMovedDuringPreparationMessage);
        var manifestStopwatch = Stopwatch.StartNew();
        var artifacts = new JsonObject();
        foreach (var name in new[] { "diff.patch", "files.json", "pull.json", "feedback.json" })
        {
            artifacts[name] = Hash(await File.ReadAllBytesAsync(Path.Combine(output, name)));
        }
        timings["manifest"] = Math.Round(manifestStopwatch.Elapsed.TotalMilliseconds, 3);
        var manifest = new JsonObject
        {
            ["version"] = 2,
            ["ready"] = true,
            ["producer"] = producer,
            ["target"] = frozen.Identity.DeepClone(),
            ["suffix"] = Suffix,
            ["sources"] = sources,
            ["guidance"] = guidance,
            ["routing"] = new JsonObject
            {
                ["path"] = RoutingPath,
                ["sha256"] = routing.Sha256,
            },
            ["guides"] = guides,
            ["policies"] = policies,
            ["context"] = context,
            ["skippedLinks"] = skippedLinks,
            ["exclusions"] = exclusions,
            ["artifacts"] = artifacts,
            ["timingsMs"] = timings,
            ["limitations"] = "Tracked Git bytes only. Symlinks, submodules and LFS pointers are inert data and cannot establish their target behavior.",
        };
        await WriteJsonAsync(output, "manifest.pending", manifest);
        File.Move(Path.Combine(output, "manifest.pending"), Path.Combine(output, "manifest.json"));
        return manifest;
    }

    private static async Task<JsonObject> CheckPreparedAsync(string output, string producer, JsonObject identity,
        JsonObject guidance, Func<Task<(JsonObject Identity, JsonObject Pull)>> freeze)
    {
        var manifest = JsonNode.Parse(await File.ReadAllBytesAsync(Path.Combine(output, "manifest.json")))!.AsObject();
        Require(manifest["version"]!.GetValue<int>() == 2 && manifest["ready"]!.GetValue<bool>()
            && manifest["producer"]!.GetValue<string>() == producer && manifest["suffix"]!.GetValue<string>() == Suffix
            && JsonEqual(manifest["target"], identity), "Prepared input is stale, mismatched, or from a different preparation version.");
        var timings = manifest["timingsMs"]?.AsObject();
        var timingNames = new List<string> { "apiFreeze", "fetch", "changedFiles", "diff", "feedback", "manifest" };
        if (manifest["sources"] is JsonObject sourceObject)
        {
            timingNames.AddRange(sourceObject.Select(pair => $"exportTree:{pair.Value!["commit"]!.GetValue<string>()}").Distinct(StringComparer.Ordinal));
        }
        var sources = manifest["sources"] as JsonObject;
        var artifacts = manifest["artifacts"] as JsonObject;
        Require(sources is not null
            && sources.Select(pair => pair.Key).Order(StringComparer.Ordinal).SequenceEqual(["baseTip", "head", "mergeBase"])
            && manifest["guidance"]?["root"]?.GetValue<string>() == "guidance"
            && manifest["routing"] is JsonObject
            && artifacts is not null
            && artifacts.Select(pair => pair.Key).Order(StringComparer.Ordinal).SequenceEqual(["diff.patch", "feedback.json", "files.json", "pull.json"])
            && manifest["guides"] is JsonArray && manifest["policies"] is JsonArray && manifest["context"] is JsonArray
            && manifest["exclusions"] is JsonArray && manifest["skippedLinks"] is JsonArray && timings is not null
            && timingNames.All(name => timings[name] is JsonValue value && value.GetValue<double>() >= 0)
            && timings.All(pair => pair.Value is JsonValue value && value.GetValue<double>() >= 0),
            "Prepared manifest omits required inputs.");
        foreach (var role in new[] { "head", "mergeBase", "baseTip" })
        {
            Require(sources![role]!["commit"]!.GetValue<string>() == identity[role]!.GetValue<string>()
                && sources[role]!["root"]!.GetValue<string>() == $"source/{identity[role]!.GetValue<string>()}",
                $"Prepared source has the wrong role: {role}");
        }
        foreach (var key in guidance["mode"]!.GetValue<string>() == "local"
            ? new[] { "mode", "originalRoot", "checkoutCommit", "workingTreeChanges", "sha256" }
            : new[] { "mode", "repository", "commit" })
        {
            Require(JsonEqual(manifest["guidance"]![key], guidance[key]), $"Prepared guidance mismatch: {key}");
        }
        foreach (var item in sources!.Select(pair => pair.Value!).Append(manifest["guidance"]!))
        {
            var root = item["root"]!.GetValue<string>();
            Require(!Path.IsPathFullyQualified(root) && !root.Split('/').Contains("..", StringComparer.Ordinal), "Invalid prepared root.");
            var actual = await DirectoryDigestAsync(Path.Combine(output, root.Replace('/', Path.DirectorySeparatorChar)));
            Require(actual["sha256"]!.GetValue<string>() == item["sha256"]!.GetValue<string>()
                && actual["files"]!.GetValue<int>() == item["files"]!.GetValue<int>(),
                $"Incomplete or modified prepared source: {root}");
        }
        foreach (var pair in artifacts!)
        {
            Require(!pair.Key.Contains('/') && Hash(await File.ReadAllBytesAsync(Path.Combine(output, pair.Key)))
                == pair.Value!.GetValue<string>(), $"Incomplete or modified input: {pair.Key}");
        }
        var changed = JsonNode.Parse(await File.ReadAllBytesAsync(Path.Combine(output, "files.json")))!.AsArray();
        var routing = await RouteGuidesAsync(Path.Combine(output, "guidance"), changed);
        Require(manifest["routing"]!["path"]!.GetValue<string>() == RoutingPath
            && manifest["routing"]!["sha256"]!.GetValue<string>() == routing.Sha256,
            "Prepared routing table changed.");
        Require(manifest["guides"]!.AsArray().Select(guide => guide!["path"]!.GetValue<string>())
            .SequenceEqual(routing.Guides, StringComparer.Ordinal), "Prepared guide routing is incomplete.");
        var included = new List<Link>();
        var context = new List<Link>();
        var skipped = new List<Link>();
        foreach (var guide in manifest["guides"]!.AsArray())
        {
            var path = guide!["path"]!.GetValue<string>();
            var body = await File.ReadAllTextAsync(Path.Combine(output, "guidance", (path + Suffix).Replace('/', Path.DirectorySeparatorChar)), Utf8NoBom);
            var actual = ValidateGuide(body, path);
            Require(actual.Topics.SequenceEqual(guide["topics"]!.AsArray().Select(item => item!.GetValue<string>()), StringComparer.Ordinal),
                $"Prepared guide topics changed: {path}");
            var links = GuideLinks(body, path);
            included.AddRange(links.Included);
            context.AddRange(links.Context);
            skipped.AddRange(links.Skipped);
        }
        Require(JsonEqual(new JsonArray(skipped.Select(LinkJson).ToArray()), manifest["skippedLinks"]),
            "Prepared guidance links are incompletely classified.");
        var expectedPolicies = new JsonArray(included.Select(LinkJson).ToArray());
        var actualPolicies = new JsonArray(manifest["policies"]!.AsArray().Select(policy =>
        {
            var copy = policy!.AsObject().DeepClone().AsObject();
            copy.Remove("body");
            return copy;
        }).ToArray());
        Require(JsonEqual(expectedPolicies, actualPolicies), "Prepared required policy inputs are incomplete.");
        var expectedContext = await ContextLinksAsync(context, Path.Combine(output, "guidance"), manifest["guidance"]?["pointers"]?.AsArray());
        Require(JsonEqual(expectedContext, manifest["context"]), "Prepared guidance context is incompletely classified or changed.");
        foreach (var policy in manifest["policies"]!.AsArray())
        {
            var path = policy!["path"]!.GetValue<string>();
            var actual = ResolvePolicy(await File.ReadAllTextAsync(Path.Combine(output, "guidance",
                (path + Suffix).Replace('/', Path.DirectorySeparatorChar)), Utf8NoBom), policy["anchor"]!.GetValue<string>(), path);
            Require(actual == policy["body"]!.GetValue<string>(), $"Prepared policy clauses changed: {path}#{policy["anchor"]!.GetValue<string>()}");
        }
        const string instructions = ".github/copilot-instructions.md";
        Require(manifest["exclusions"]!.AsArray().Count == 2
            && manifest["exclusions"]![1]!["body"]!.GetValue<string>() == ResolvePolicy(
                await File.ReadAllTextAsync(Path.Combine(output, "guidance",
                    (instructions + Suffix).Replace('/', Path.DirectorySeparatorChar)), Utf8NoBom),
                "security-concerns-are-out-of-scope", instructions),
            "Prepared exclusions do not match the trusted instruction snapshot.");
        Require(JsonEqual((await freeze()).Identity, manifest["target"]),
            "The target or base branch moved during validation.");
        return manifest;
    }

    private static async Task WriteJsonAsync(string directory, string name, JsonNode value)
    {
        await WriteAsync(directory, name, SerializeJson(value));
    }

    internal static string SerializeJson(JsonNode value) => SerializeJson(value, indented: true) + "\n";

    private static bool JsonEqual(JsonNode? left, JsonNode? right) =>
        SerializeJson(left, indented: false) == SerializeJson(right, indented: false);

    private static string SerializeJson(JsonNode? value, bool indented)
    {
        var builder = new StringBuilder();
        WriteJsonNode(builder, value, indented, depth: 0);
        return builder.ToString();
    }

    private static void WriteJsonNode(StringBuilder builder, JsonNode? node, bool indented, int depth)
    {
        switch (node)
        {
            case null:
                builder.Append("null");
                return;
            case JsonObject jsonObject:
                builder.Append('{');
                var propertyIndex = 0;
                foreach (var property in jsonObject)
                {
                    if (propertyIndex++ > 0)
                    {
                        builder.Append(',');
                    }
                    WriteJsonSeparator(builder, indented, depth + 1);
                    WriteJsonString(builder, property.Key);
                    builder.Append(indented ? ": " : ":");
                    WriteJsonNode(builder, property.Value, indented, depth + 1);
                }
                if (propertyIndex > 0)
                {
                    WriteJsonSeparator(builder, indented, depth);
                }
                builder.Append('}');
                return;
            case JsonArray jsonArray:
                builder.Append('[');
                var itemIndex = 0;
                foreach (var item in jsonArray)
                {
                    if (itemIndex++ > 0)
                    {
                        builder.Append(',');
                    }
                    WriteJsonSeparator(builder, indented, depth + 1);
                    WriteJsonNode(builder, item, indented, depth + 1);
                }
                if (itemIndex > 0)
                {
                    WriteJsonSeparator(builder, indented, depth);
                }
                builder.Append(']');
                return;
            case JsonValue jsonValue:
                if (!jsonValue.TryGetValue<JsonElement>(out var element))
                {
                    element = JsonSerializer.SerializeToElement(jsonValue, NodeValueJsonOptions);
                }
                WriteJsonElement(builder, element, indented, depth);
                return;
            default:
                throw new InvalidOperationException($"Unsupported JSON node type: {node.GetType().FullName}");
        }
    }

    private static void WriteJsonElement(StringBuilder builder, JsonElement element, bool indented, int depth)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                builder.Append('{');
                var propertyIndex = 0;
                foreach (var property in element.EnumerateObject())
                {
                    if (propertyIndex++ > 0)
                    {
                        builder.Append(',');
                    }
                    WriteJsonSeparator(builder, indented, depth + 1);
                    WriteJsonString(builder, property.Name);
                    builder.Append(indented ? ": " : ":");
                    WriteJsonElement(builder, property.Value, indented, depth + 1);
                }
                if (propertyIndex > 0)
                {
                    WriteJsonSeparator(builder, indented, depth);
                }
                builder.Append('}');
                break;
            case JsonValueKind.Array:
                builder.Append('[');
                var itemIndex = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (itemIndex++ > 0)
                    {
                        builder.Append(',');
                    }
                    WriteJsonSeparator(builder, indented, depth + 1);
                    WriteJsonElement(builder, item, indented, depth + 1);
                }
                if (itemIndex > 0)
                {
                    WriteJsonSeparator(builder, indented, depth);
                }
                builder.Append(']');
                break;
            case JsonValueKind.String:
                WriteJsonString(builder, element.GetString()!);
                break;
            case JsonValueKind.Number:
                builder.Append(element.GetRawText());
                break;
            case JsonValueKind.True:
                builder.Append("true");
                break;
            case JsonValueKind.False:
                builder.Append("false");
                break;
            case JsonValueKind.Null:
                builder.Append("null");
                break;
            default:
                throw new InvalidOperationException($"Unsupported JSON value kind: {element.ValueKind}");
        }
    }

    private static void WriteJsonSeparator(StringBuilder builder, bool indented, int depth)
    {
        if (!indented)
        {
            return;
        }
        builder.Append('\n');
        builder.Append(' ', depth * 2);
    }

    private static void WriteJsonString(StringBuilder builder, string value)
    {
        builder.Append('"');
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            switch (character)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (character < 0x20 || char.IsSurrogate(character)
                        && (char.IsLowSurrogate(character) || index + 1 == value.Length || !char.IsLowSurrogate(value[index + 1])))
                    {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                        if (char.IsHighSurrogate(character))
                        {
                            builder.Append(value[++index]);
                        }
                    }
                    break;
            }
        }
        builder.Append('"');
    }

    private static long JsonInteger(JsonNode value)
    {
        Require(TryJsonSafeInteger(value, out var result), "Expected a safe integer.");
        return result;
    }

    private static bool TryJsonSafeInteger(JsonNode? value, out long result)
    {
        result = 0;
        if (value is null || !double.TryParse(value.ToJsonString(), NumberStyles.Float,
            CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number)
            || number != Math.Truncate(number) || Math.Abs(number) > 9_007_199_254_740_991)
        {
            return false;
        }
        result = checked((long)number);
        return true;
    }

    [GeneratedRegex("^[a-f0-9]{40}$", RegexOptions.CultureInvariant)]
    private static partial Regex FullShaRegex();
    [GeneratedRegex("^[a-z0-9_.-]+/[a-z0-9_.-]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RepositoryNameRegex();
    [GeneratedRegex("^[1-9][0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex PositiveIntegerRegex();
    [GeneratedRegex("^[a-z0-9.-]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HostnameRegex();
    [GeneratedRegex("[<>:\"\\\\|?*\\x00-\\x1f]", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidPathPartRegex();
    [GeneratedRegex("[ .]$", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidPathEndRegex();
    [GeneratedRegex("^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\\.|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReservedWindowsNameRegex();
    [GeneratedRegex("^[-*] \\S", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex BulletRegex();
    [GeneratedRegex("<[^>]*>", RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();
    [GeneratedRegex("[^a-z0-9 _-]", RegexOptions.CultureInvariant)]
    private static partial Regex NonAnchorRegex();
    [GeneratedRegex("\\[[^\\]]+\\]\\(\\s*(?:<([^>]+)>|([^\\s)]+))(?:\\s+(?:\"[^\"]*\"|'[^']*'))?\\s*\\)", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownLinkRegex();
    [GeneratedRegex("\\.md(?:#.*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownDestinationRegex();
    [GeneratedRegex("^[a-z][a-z0-9+.-]*:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SchemeRegex();
    [GeneratedRegex("^[a-z0-9-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidAnchorRegex();
    [GeneratedRegex("\\b(supplemental|implementation/test references)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SkippedLineRegex();
    [GeneratedRegex("^(#{1,6}) (.+)$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex HeadingRegex();
    [GeneratedRegex("^(?:https://github\\.com/|git@github\\.com:)([a-z0-9_.-]+/[a-z0-9_.-]+?)(?:\\.git)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RemoteRegex();
}
