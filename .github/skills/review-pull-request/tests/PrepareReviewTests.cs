// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Xunit;

public class PrepareReviewTests
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string RepositoryRoot = Path.GetFullPath("../../../../", Path.GetDirectoryName(SourcePath())!);
    private static readonly string TestArtifacts = Path.Combine(RepositoryRoot, "artifacts", "prepare-review-tests");
    private static readonly string ProducerSourcePath = Path.Combine(
        RepositoryRoot, ".github/skills/review-pull-request/scripts/prepare-review.cs");
    private static readonly Dictionary<string, string> Identity = new()
    {
        ["GIT_AUTHOR_NAME"] = "Preparation test",
        ["GIT_AUTHOR_EMAIL"] = "preparation@example.invalid",
        ["GIT_COMMITTER_NAME"] = "Preparation test",
        ["GIT_COMMITTER_EMAIL"] = "preparation@example.invalid",
    };

    [Fact]
    public void ProducerGitArgumentsEnableWindowsLongPathsBeforeTheSubcommand()
    {
        foreach (var arguments in new[]
        {
            new[] { "--version" },
            new[] { "-C", "checkout", "status" },
            new[] { "--git-dir", "store", "config" },
            new[] { "--git-dir", "store", "cat-file", "--batch" },
            new[] { "init", "--bare", "store" },
            new[] { "remote", "get-url", "origin" },
        })
        {
            Assert.Equal(new[] { "-c", "core.longpaths=true" }.Concat(arguments), PrepareReviewProgram.GitArguments(arguments));
        }
    }

    [Fact]
    public async Task PreparesDistinctCompleteSidesInertTargetInstructionsLargeFilesAndDirtyGuidance()
    {
        await using var fixture = await Fixture.CreateAsync();
        var checkoutBefore = fixture.Git(fixture.GuidanceRoot, "status", "--porcelain");
        var manifest = await fixture.PrepareAsync();
        Assert.Equal(checkoutBefore, fixture.Git(fixture.GuidanceRoot, "status", "--porcelain"));
        Assert.Equal(fixture.Head, manifest["target"]!["head"]!.GetValue<string>());
        Assert.Equal(fixture.MergeBase, manifest["target"]!["mergeBase"]!.GetValue<string>());
        Assert.Equal(fixture.BaseTip, manifest["target"]!["baseTip"]!.GetValue<string>());
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(ProducerSourcePath))),
            manifest["producer"]!.GetValue<string>());
        Assert.NotEqual(fixture.Head, fixture.BaseTip);
        Assert.NotEqual(fixture.BaseTip, fixture.MergeBase);
        Assert.Equal("HEAD_VALUE\n", await fixture.SourceAsync(manifest, "head", "src/Value.cs"));
        Assert.Equal("MERGE_DEPENDENCY\n", await fixture.SourceAsync(manifest, "mergeBase", "src/Unchanged.cs"));
        Assert.Equal("BASE_TIP_DEPENDENCY\n", await fixture.SourceAsync(manifest, "baseTip", "src/Unchanged.cs"));
        Assert.Equal("DELETED_BYTES\n", await fixture.SourceAsync(manifest, "mergeBase", "src/Deleted.cs"));
        Assert.Equal("RENAMED_BYTES\n", await fixture.SourceAsync(manifest, "head", "src/Renamed.cs"));
        Assert.Equal("Value.cs", await fixture.SourceAsync(manifest, "head", "src/Mode.cs"));
        Assert.Equal("TARGET_INSTRUCTION_SENTINEL\n", await fixture.SourceAsync(manifest, "head", "AGENTS.md"));
        Assert.Equal("TARGET_ROOT_INSTRUCTION_SENTINEL\n", await fixture.SourceAsync(manifest, "head", ".github/copilot-instructions.md"));
        Assert.Contains("RELEVANT_IMPLEMENTATION", await fixture.SourceAsync(manifest, "head", "src/Large.cs"));
        Assert.True(File.Exists(Path.Combine(fixture.Output, manifest["sources"]!["head"]!["root"]!.GetValue<string>(), "src/Mode.cs.source")));
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(Path.Combine(
                fixture.Output, manifest["sources"]!["head"]!["root"]!.GetValue<string>(), "src/Mode.cs.source")));
        }
        Assert.False(File.Exists(Path.Combine(fixture.Output, manifest["sources"]!["head"]!["root"]!.GetValue<string>(), "AGENTS.md")));
        Assert.True(manifest["guidance"]!["workingTreeChanges"]!.GetValue<bool>());
        Assert.Equal(".github/skills/review-pull-request/routing.md",
            manifest["routing"]!["path"]!.GetValue<string>());
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(
            fixture.Output, "guidance/.github/skills/review-pull-request/routing.md.source")))),
            manifest["routing"]!["sha256"]!.GetValue<string>());
        Assert.Equal(2, manifest["exclusions"]!.AsArray().Count);
        Assert.Contains("Do not review the excluded scope", manifest["exclusions"]![1]!["body"]!.GetValue<string>());
        foreach (var name in new[] { "apiFreeze", "fetch", "changedFiles", "diff", "feedback", "manifest" })
        {
            Assert.True(manifest["timingsMs"]![name]!.GetValue<double>() >= 0);
        }
        Assert.Equal(3, manifest["timingsMs"]!.AsObject().Count(pair => pair.Key.StartsWith("exportTree:", StringComparison.Ordinal)));
        Assert.Equal("""{"comments":[],"reviews":[],"inline":[]}""",
            JsonSerializer.Serialize(JsonNode.Parse(await File.ReadAllBytesAsync(Path.Combine(fixture.Output, "feedback.json")))));
        Assert.Contains("DIRTY_GUIDANCE", await File.ReadAllTextAsync(Path.Combine(fixture.Output,
            "guidance/docs/CrossCuttingGuidance.md.source"), Utf8NoBom));
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData("main")]
    [InlineData("release/11.0")]
    public async Task KeepsBindingBaseTipSeparateFromMergeBase(string baseRef)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Pull["base"]!["ref"] = baseRef;
        var manifest = await fixture.PrepareAsync();
        Assert.Equal(baseRef, manifest["target"]!["baseRef"]!.GetValue<string>());
        Assert.Equal(fixture.BaseTip, manifest["target"]!["baseTip"]!.GetValue<string>());
        Assert.Equal(fixture.MergeBase, manifest["target"]!["mergeBase"]!.GetValue<string>());
    }

    [Fact]
    public async Task IncludesExactRequiredPolicySectionFromSelectedGuidanceSnapshot()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.WriteGuidanceAsync("docs/Policy.md",
            "# Policy\n## Required clause\n- Only this requirement.\n## Other clause\n- Unrelated.\n");
        await fixture.WriteGuidanceAsync("docs/CrossCuttingGuidance.md",
            "# Guidance\n## Overarching principles\n- Follow [the requirement](Policy.md#required-clause).\n" +
            "## Topics\n### Topic\n- Review changed code.\n");
        var manifest = await fixture.PrepareAsync();
        var policy = Assert.Single(manifest["policies"]!.AsArray())!;
        Assert.Equal("docs/Policy.md", policy["path"]!.GetValue<string>());
        Assert.Equal("required-clause", policy["anchor"]!.GetValue<string>());
        Assert.Equal("## Required clause\n- Only this requirement.", policy["body"]!.GetValue<string>());
        manifest["policies"] = new JsonArray();
        await File.WriteAllTextAsync(Path.Combine(fixture.Output, "manifest.json"), JsonSerializer.Serialize(manifest), Utf8NoBom);
        await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
    }

    [Fact]
    public async Task MissingDelegatedPolicyAnchorFailsBeforePublishingReadiness()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.WriteGuidanceAsync("docs/Policy.md", "# Policy\n## Different\n- A rule.\n");
        await fixture.WriteGuidanceAsync("docs/CrossCuttingGuidance.md",
            "# Guidance\n## Overarching principles\n- Follow [the requirement](Policy.md#missing).\n" +
            "## Topics\n### Topic\n- Review changed code.\n");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.PrepareAsync);
        Assert.Contains("Missing or ambiguous required policy", exception.Message);
        Assert.False(File.Exists(Path.Combine(fixture.Output, "manifest.json")));
    }

    [Fact]
    public void ClassifiesEveryRepositoryRelativeMarkdownLinkInEachRoutedGuide()
    {
        foreach (var name in new[] { "CrossCuttingGuidance.md", "BlazorComponentsGuidance.md" })
        {
            var body = File.ReadAllText(Path.Combine(RepositoryRoot, "docs", name));
            var classified = PrepareReviewProgram.GuideLinks(body, $"docs/{name}");
            var count = Regex.Matches(body, @"\[[^\]]+\]\((?:\.\.?/)*[^)\s]+\.md(?:#[^)\s]*)?\)").Count;
            Assert.Equal(count, classified.Included.Count + classified.Context.Count + classified.Skipped.Count);
            Assert.All(classified.Context, link => Assert.Equal("context", link.Role));
            Assert.All(classified.Skipped, link => Assert.NotNull(link.Reason));
        }
        var architecture = PrepareReviewProgram.GuideLinks(
            File.ReadAllText(Path.Combine(RepositoryRoot, "docs/BlazorComponentsGuidance.md")),
            "docs/BlazorComponentsGuidance.md");
        var context = Assert.Single(architecture.Context);
        Assert.Equal("src/Components/ARCHITECTURE.md", context.Path);
        Assert.Contains("src/Components/AGENTS.md#code-clarity-and-durable-knowledge",
            architecture.Included.Select(link => $"{link.Path}#{link.Anchor}"));
        Assert.Contains("src/Components/AGENTS.md#cross-runtime-design-checkpoint",
            architecture.Included.Select(link => $"{link.Path}#{link.Anchor}"));
        Assert.Contains("src/Components/AGENTS.md#creating-e2e-tests",
            architecture.Included.Select(link => $"{link.Path}#{link.Anchor}"));
        var sample = "- Apply [binding](Policy.md#binding).\n" +
            "- Orient with [architecture](../src/Components/ARCHITECTURE.md).\n" +
            "- Read [design](<../src/Components/DESIGN.md> \"Context\").\n" +
            "- External [docs](https://example.com/Policy.md) are not repository-relative.\n" +
            "- Supplemental implementation/test references: [example](Example.md#sample).\n" +
            "- For Components APIs follow [API](../src/Components/AGENTS.md#code-clarity-and-durable-knowledge); generic JSInterop differs.\n";
        var links = PrepareReviewProgram.GuideLinks(sample, "docs/Guide.md");
        Assert.Equal(["binding", "code-clarity-and-durable-knowledge"], links.Included.Select(link => link.Anchor));
        Assert.Equal(["src/Components/ARCHITECTURE.md", "src/Components/DESIGN.md"], links.Context.Select(link => link.Path));
        Assert.Equal(["sample"], links.Skipped.Select(link => link.Anchor));
        var mixed = File.ReadAllLines(Path.Combine(RepositoryRoot, "docs/BlazorComponentsGuidance.md"))
            .Single(line => line.Contains("For Components E2E work", StringComparison.Ordinal));
        var mixedLinks = PrepareReviewProgram.GuideLinks(mixed, "docs/BlazorComponentsGuidance.md");
        Assert.Equal([
            "CONTRIBUTING.md#tests",
            ".github/copilot-instructions.md#running-tests",
            "src/Components/AGENTS.md#creating-e2e-tests",
        ], mixedLinks.Included.Select(link => $"{link.Path}#{link.Anchor}"));
        Assert.Empty(mixedLinks.Skipped);
        Assert.Throws<InvalidOperationException>(() =>
            PrepareReviewProgram.GuideLinks("[bad](Policy.md#)", "docs/Guide.md"));
    }

    [Theory]
    [InlineData("code-clarity-and-durable-knowledge", "Code Clarity and Durable Knowledge")]
    [InlineData("cross-runtime-design-checkpoint", "Cross-Runtime Design Checkpoint")]
    [InlineData("creating-e2e-tests", "Creating E2E Tests")]
    public async Task IncludesPoliciesLinkedByDifferentlyNamedRoutedGuide(string anchor, string heading)
    {
        await using var fixture = await Fixture.CreateAsync(components: true);
        await fixture.WriteGuidanceAsync(".github/skills/review-pull-request/routing.md",
            "| Changed path prefix | Guide |\n| --- | --- |\n" +
            "| * | docs/CrossCuttingGuidance.md |\n" +
            "| src/Components/ | docs/SpecializedGuidance.md |\n");
        await fixture.WriteGuidanceAsync("docs/SpecializedGuidance.md",
            "# Specialized\n## Overarching principles\n- A principle.\n" +
            "## Topics\n### Tests\n" +
            $"- Follow [the delegated policy](../src/Components/AGENTS.md#{anchor}).\n");
        await fixture.WriteGuidanceAsync("src/Components/AGENTS.md",
            $"# Components\n## {heading}\n- Validate the behavior.\n");

        var manifest = await fixture.PrepareAsync();

        Assert.Equal([
            "docs/CrossCuttingGuidance.md",
            "docs/SpecializedGuidance.md",
        ], manifest["guides"]!.AsArray().Select(guide => guide!["path"]!.GetValue<string>()));
        Assert.Empty(manifest["skippedLinks"]!.AsArray());
        var policy = Assert.Single(manifest["policies"]!.AsArray())!.AsObject();
        Assert.Equal("src/Components/AGENTS.md", policy["path"]!.GetValue<string>());
        Assert.Equal(anchor, policy["anchor"]!.GetValue<string>());
        Assert.Equal("docs/SpecializedGuidance.md", policy["guide"]!.GetValue<string>());
        Assert.Equal($"## {heading}\n- Validate the behavior.", policy["body"]!.GetValue<string>());
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());

        manifest["policies"] = new JsonArray();
        await fixture.WriteManifestAsync(manifest);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
        Assert.Contains("Prepared required policy inputs are incomplete.", exception.Message);
    }

    [Theory]
    [InlineData("Components", true)]
    [InlineData("ComponentsX", false)]
    public async Task RoutesARenameUsingItsPreviousPath(string area, bool components)
    {
        await using var fixture = await Fixture.CreateAsync();
        var oldPath = $"src/{area}/Old.cs";
        const string newPath = "docs/Renamed.cs";
        var original = fixture.Commit(new Dictionary<string, object> { [oldPath] = "UNCHANGED_VALUE\n" });
        var head = fixture.Commit(new Dictionary<string, object> { [newPath] = "UNCHANGED_VALUE\n" }, original);
        fixture.Pull["base"]!["ref"] = "main";
        fixture.Pull["changed_files"] = 1;
        fixture.Pull["head"]!["sha"] = head;
        fixture.BaseTip = original;
        fixture.MergeBase = original;
        fixture.Diff = fixture.GitBytes(fixture.Repository, "diff", "--binary", original, head);
        fixture.Files = new JsonArray(new JsonObject
        {
            ["filename"] = newPath,
            ["previous_filename"] = oldPath,
            ["status"] = "renamed",
            ["sha"] = fixture.Git(fixture.Repository, "rev-parse", $"{head}:{newPath}"),
        });
        await fixture.WriteGuidanceAsync("docs/BlazorComponentsGuidance.md",
            "# Components\n## Overarching principles\n- A rule.\n" +
            "## Topics\n### Tests\n- Follow [Components E2E](../src/Components/AGENTS.md#creating-e2e-tests).\n");
        await fixture.WriteGuidanceAsync("src/Components/AGENTS.md",
            "# Components\n## Creating E2E Tests\n- Validate the behavior.\n");
        var manifest = await fixture.PrepareAsync();
        Assert.Equal(components ? 2 : 1, manifest["guides"]!.AsArray().Count);
        Assert.Equal(components ? 1 : 0, manifest["policies"]!.AsArray().Count);
        Assert.Empty(manifest["skippedLinks"]!.AsArray());
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());
        var filename = Path.Combine(fixture.Output, "files.json");
        var changed = JsonNode.Parse(await File.ReadAllBytesAsync(filename))!.AsArray();
        changed[0]!.AsObject().Remove("previous_filename");
        var bytes = Utf8NoBom.GetBytes(JsonSerializer.Serialize(changed, JsonOptions) + "\n");
        await File.WriteAllBytesAsync(filename, bytes);
        manifest["artifacts"]!["files.json"] = Convert.ToHexStringLower(SHA256.HashData(bytes));
        await File.WriteAllTextAsync(Path.Combine(fixture.Output, "manifest.json"), JsonSerializer.Serialize(manifest), Utf8NoBom);
        if (components)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
        }
        else
        {
            Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());
        }
    }

    [Theory]
    [InlineData(false, "docs/CrossCuttingGuidance.md")]
    [InlineData(true, "docs/CrossCuttingGuidance.md", "docs/BlazorComponentsGuidance.md")]
    public async Task CurrentRoutingTablePreservesExistingGuideSelection(bool components, params string[] expected)
    {
        await using var fixture = await Fixture.CreateAsync(components);
        var manifest = await fixture.PrepareAsync();
        Assert.Equal(expected, manifest["guides"]!.AsArray().Select(guide => guide!["path"]!.GetValue<string>()));
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());
    }

    public static TheoryData<string, string> MalformedRoutingTables => new()
    {
        { "missing", "Required routing table is missing" },
        { "empty", "Required routing table is empty or malformed" },
        { "malformed", "Required routing table is empty or malformed" },
        { "bad prefix", "Every routing prefix except * must end in /" },
        { "no star", "Required routing table needs a * row" },
        { "duplicate", "Required routing table has a duplicate row" },
        { "missing guide", "Required guide docs/MissingGuidance.md is missing" },
        { "invalid guide", "Required guide docs/InvalidGuidance.md needs exactly one ## Overarching principles section" },
    };

    [Theory]
    [MemberData(nameof(MalformedRoutingTables))]
    public async Task MalformedRoutingTableReturnsBlocked(string mutation, string expected)
    {
        await using var fixture = await Fixture.CreateAsync();
        var routing = Path.Combine(fixture.GuidanceRoot, ".github/skills/review-pull-request/routing.md");
        switch (mutation)
        {
            case "missing":
                File.Delete(routing);
                break;
            case "empty":
                await File.WriteAllTextAsync(routing, string.Empty, Utf8NoBom);
                break;
            case "malformed":
                await File.WriteAllTextAsync(routing,
                    "| Changed path prefix | Guide |\n| --- |\n| * | docs/CrossCuttingGuidance.md |\n", Utf8NoBom);
                break;
            case "bad prefix":
                await File.WriteAllTextAsync(routing,
                    "| Changed path prefix | Guide |\n| --- | --- |\n" +
                    "| * | docs/CrossCuttingGuidance.md |\n| src/Components | docs/BlazorComponentsGuidance.md |\n", Utf8NoBom);
                break;
            case "no star":
                await File.WriteAllTextAsync(routing,
                    "| Changed path prefix | Guide |\n| --- | --- |\n| src/ | docs/CrossCuttingGuidance.md |\n", Utf8NoBom);
                break;
            case "duplicate":
                await File.WriteAllTextAsync(routing,
                    "| Changed path prefix | Guide |\n| --- | --- |\n" +
                    "| * | docs/CrossCuttingGuidance.md |\n| * | docs/CrossCuttingGuidance.md |\n", Utf8NoBom);
                break;
            case "missing guide":
                await File.WriteAllTextAsync(routing,
                    "| Changed path prefix | Guide |\n| --- | --- |\n| * | docs/MissingGuidance.md |\n", Utf8NoBom);
                break;
            case "invalid guide":
                await fixture.WriteGuidanceAsync("docs/InvalidGuidance.md",
                    "# Invalid\n## Topics\n### Topic\n- A rule.\n");
                await File.WriteAllTextAsync(routing,
                    "| Changed path prefix | Guide |\n| --- | --- |\n| * | docs/InvalidGuidance.md |\n", Utf8NoBom);
                break;
        }

        var output = new StringWriter();
        var error = new StringWriter();
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = await PrepareReviewProgram.RunAsync([
                "--repo", "owner/product",
                "--pr", "42",
                "--output", fixture.Output,
                "--guidance-root", fixture.GuidanceRoot,
            ], fixture.CreateDependencies());
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
        Assert.Equal(string.Empty, output.ToString());
        Assert.StartsWith($"BLOCKED: {expected}", error.ToString(), StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(fixture.Output, "manifest.json")));
    }

    [Fact]
    public async Task RoutesEveryMatchingRowAndDeduplicatesGuides()
    {
        await using var fixture = await Fixture.CreateAsync(components: true);
        await fixture.WriteGuidanceAsync(".github/skills/review-pull-request/routing.md",
            "| Changed path prefix | Guide |\n| --- | --- |\n" +
            "| * | docs/CrossCuttingGuidance.md |\n" +
            "| src/ | docs/BlazorComponentsGuidance.md |\n" +
            "| src/Components/ | docs/CrossCuttingGuidance.md |\n");
        var manifest = await fixture.PrepareAsync();
        Assert.Equal(["docs/CrossCuttingGuidance.md", "docs/BlazorComponentsGuidance.md"],
            manifest["guides"]!.AsArray().Select(guide => guide!["path"]!.GetValue<string>()));
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());
    }

    [Fact]
    public async Task ReleaseBaseUsesRoutingFromTrustedGuidanceSnapshot()
    {
        await using var fixture = await Fixture.CreateAsync(components: true);
        fixture.Pull["base"]!["ref"] = "release/11.0";
        var commit = fixture.Commit(new Dictionary<string, object>
        {
            [".github/skills/review-pull-request/routing.md"] =
                "| Changed path prefix | Guide |\n| --- | --- |\n| * | docs/CrossCuttingGuidance.md |\n",
            ["docs/CrossCuttingGuidance.md"] =
                "# Guidance\n## Overarching principles\n- A principle.\n## Topics\n### Topic\n- A rule.\n",
            [".github/copilot-instructions.md"] =
                "# Instructions\n## Security Concerns Are Out of Scope\nDo not review the excluded scope.\n",
        });
        var options = fixture.Options with { GuidanceRoot = null, Guidance = $"reviewer/guidance@{commit}" };
        var manifest = await fixture.PrepareAsync(options);
        Assert.Equal(["docs/CrossCuttingGuidance.md"],
            manifest["guides"]!.AsArray().Select(guide => guide!["path"]!.GetValue<string>()));
        Assert.Equal("release/11.0", manifest["target"]!["baseRef"]!.GetValue<string>());
        Assert.True((await fixture.CheckAsync(options))["ready"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData("table")]
    [InlineData("hash")]
    public async Task CheckRejectsAChangedOrTamperedRoutingTable(string mutation)
    {
        await using var fixture = await Fixture.CreateAsync();
        var manifest = await fixture.PrepareAsync();
        if (mutation == "table")
        {
            await File.AppendAllTextAsync(Path.Combine(fixture.Output,
                "guidance/.github/skills/review-pull-request/routing.md.source"), "\nCHANGED\n", Utf8NoBom);
        }
        else
        {
            manifest["routing"]!["sha256"] = new string('0', 64);
            await fixture.WriteManifestAsync(manifest);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
    }

    [Theory]
    [InlineData("main")]
    [InlineData("release/11.0")]
    public async Task IncludesReadableComponentsArchitectureContextFromSelectedGuidance(string baseRef)
    {
        await using var fixture = await Fixture.CreateAsync(components: true);
        fixture.Pull["base"]!["ref"] = baseRef;
        await fixture.WriteGuidanceAsync("src/Components/ARCHITECTURE.md", "REVIEWER_WORKING_TREE_ARCHITECTURE\n");
        await fixture.WriteGuidanceAsync("docs/BlazorComponentsGuidance.md",
            "# Components\n[Architecture](../src/Components/ARCHITECTURE.md)\n" +
            "## Overarching principles\n- Apply the full guide.\n## Topics\n### Forms\n- Review binding.\n");
        var options = fixture.Options;
        var architecture = "REVIEWER_WORKING_TREE_ARCHITECTURE\n";
        if (baseRef == "release/11.0")
        {
            architecture = "IMMUTABLE_REVIEWER_ARCHITECTURE\n";
            var commit = fixture.Commit(new Dictionary<string, object>
            {
                [".github/skills/review-pull-request/routing.md"] =
                    "| Changed path prefix | Guide |\n| --- | --- |\n" +
                    "| * | docs/CrossCuttingGuidance.md |\n| src/Components/ | docs/BlazorComponentsGuidance.md |\n",
                ["docs/CrossCuttingGuidance.md"] = "# Guidance\n## Overarching principles\n- A principle.\n## Topics\n### Topic\n- A rule.\n",
                ["docs/BlazorComponentsGuidance.md"] = "# Components\n[Architecture](../src/Components/ARCHITECTURE.md)\n## Overarching principles\n- A principle.\n## Topics\n### Forms\n- A rule.\n",
                ["src/Components/ARCHITECTURE.md"] = architecture,
                [".github/copilot-instructions.md"] = "# Instructions\n## Security Concerns Are Out of Scope\nDo not review the excluded scope.\n",
            });
            options = options with { GuidanceRoot = null, Guidance = $"reviewer/guidance@{commit}" };
        }
        var manifest = await fixture.PrepareAsync(options);
        Assert.Equal(2, manifest["guides"]!.AsArray().Count);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(fixture.Output, "guidance/src/Components/ARCHITECTURE.md.source"));
        Assert.Equal(architecture, Utf8NoBom.GetString(bytes));
        Assert.Equal(baseRef == "main" ? "local" : "remote", manifest["guidance"]!["mode"]!.GetValue<string>());
        if (baseRef == "release/11.0")
        {
            Assert.False(File.Exists(Path.Combine(fixture.Output, manifest["sources"]!["baseTip"]!["root"]!.GetValue<string>(),
                "docs/BlazorComponentsGuidance.md.source")));
        }
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), Assert.Single(manifest["context"]!.AsArray())!["sha256"]!.GetValue<string>());
        Assert.True((await fixture.CheckAsync(options))["ready"]!.GetValue<bool>());
    }

    [Fact]
    public void SerializesJavaScriptCompatibleJsonBytes()
    {
        var value = JsonNode.Parse("""
            {
              "nested": {
                "emoji": "\uD83D\uDFE1 \uD83D\uDCA1 \uD83D\uDD75\uFE0F \uD83E\uDD16",
                "narrowSpace": "\u202F",
                "html": "<>&",
                "array": ["\uD83D\uDFE1", {"value": "\uD83D\uDCA1"}]
              },
              "a": 1
            }
            """)!;
        Assert.Equal("🟡 💡 🕵️ 🤖", value["nested"]!["emoji"]!.GetValue<string>());
        var expected = """
            {
              "nested": {
                "emoji": "🟡 💡 🕵️ 🤖",
                "narrowSpace": " ",
                "html": "<>&",
                "array": [
                  "🟡",
                  {
                    "value": "💡"
                  }
                ]
              },
              "a": 1
            }

            """.ReplaceLineEndings("\n");
        Assert.Equal(expected, PrepareReviewProgram.SerializeJson(value));
    }

    [Fact]
    public async Task SuccessfulCliWritesTheCompactResultJson()
    {
        await using var fixture = await Fixture.CreateAsync();
        var output = new StringWriter();
        var error = new StringWriter();
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = await PrepareReviewProgram.RunAsync([
                "--repo", "owner/product",
                "--pr", "42",
                "--output", fixture.Output,
                "--guidance-root", fixture.GuidanceRoot,
            ], fixture.CreateDependencies());
            Assert.Equal(0, exitCode);
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
        Assert.Equal(string.Empty, error.ToString());
        var line = output.ToString();
        Assert.EndsWith("\n", line, StringComparison.Ordinal);
        Assert.DoesNotContain('\n', line.TrimEnd('\n'));
        var result = JsonNode.Parse(line)!.AsObject();
        Assert.True(result["ready"]!.GetValue<bool>());
        Assert.Equal(42, result["target"]!["pr"]!.GetValue<int>());
        Assert.Equal(Path.Combine(Path.GetFullPath(fixture.Output), "manifest.json"),
            result["manifest"]!.GetValue<string>());
    }

    [Fact]
    public async Task FailedChildStderrPrecedesBlockedCliMessage()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        await using var fixture = await Fixture.CreateAsync();
        var fakeGh = Path.Combine(fixture.Root, "gh");
        await File.WriteAllTextAsync(fakeGh,
            "#!/bin/sh\nprintf 'gh: Bad credentials (HTTP 401)\\n' >&2\nexit 1\n", Utf8NoBom);
        File.SetUnixFileMode(fakeGh,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        var output = new StringWriter();
        var error = new StringWriter();
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        try
        {
            Environment.SetEnvironmentVariable("PATH", $"{fixture.Root}{Path.PathSeparator}{originalPath}");
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = await PrepareReviewProgram.RunAsync([
                "--repo", "owner/product",
                "--pr", "42",
                "--output", fixture.Output,
                "--guidance-root", fixture.GuidanceRoot,
            ], fixture.CreateDependencies());
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
        Assert.Equal(string.Empty, output.ToString());
        Assert.Equal(
            "gh: Bad credentials (HTTP 401)\n" +
            "BLOCKED: gh failed: gh: Bad credentials (HTTP 401)\n",
            error.ToString());
    }

    [Fact]
    public async Task KillsAChildWhenProcessOutputExceedsTheLimit()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        await using var fixture = await Fixture.CreateAsync();
        var child = Path.Combine(fixture.Root, "oversized-output");
        await File.WriteAllTextAsync(child,
            "#!/bin/sh\nprintf '%1024s' ''\nprintf '%1024s' ''\nsleep 5\n", Utf8NoBom);
        File.SetUnixFileMode(child,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var stopwatch = Stopwatch.StartNew();
        var exception = Assert.Throws<InvalidOperationException>(() => PrepareReviewProgram.Run(
            child,
            [],
            maximumProcessOutputBytes: 1024));
        stopwatch.Stop();
        Assert.Equal($"{child} failed: process output exceeded 64 MiB.", exception.Message);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Child was not killed promptly: {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task ExistingOutputDirectoryUsesNativeCliMessage()
    {
        await using var fixture = await Fixture.CreateAsync();
        Directory.CreateDirectory(fixture.Output);
        var output = new StringWriter();
        var error = new StringWriter();
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = await PrepareReviewProgram.RunAsync([
                "--repo", "owner/product",
                "--pr", "42",
                "--output", fixture.Output,
                "--guidance-root", fixture.GuidanceRoot,
            ], fixture.CreateDependencies());
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
        Assert.Equal(string.Empty, output.ToString());
        Assert.Equal(
            $"BLOCKED: Output directory already exists: {Path.GetFullPath(fixture.Output)}",
            error.ToString().TrimEnd('\r', '\n'));
    }

    [Fact]
    public async Task FetchesRepositoryGroupsSequentiallyInInsertionOrder()
    {
        await using var fixture = await Fixture.CreateAsync();
        var original = fixture.CreateDependencies();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repositories = new List<string>();
        var active = 0;
        var overlap = false;
        async Task Fetch(string repository, IReadOnlyList<string> commits, string store)
        {
            if (Interlocked.Increment(ref active) != 1)
            {
                overlap = true;
            }
            repositories.Add(repository);
            try
            {
                if (repositories.Count == 1)
                {
                    firstStarted.SetResult();
                    await releaseFirst.Task;
                }
                await original.Fetch!(repository, commits, store);
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        }
        var preparation = PrepareReviewProgram.PrepareAsync(
            fixture.Options, new PrepareReviewProgram.Dependencies(original.Api, Fetch, original.ProducerSourcePath));
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(100);
        var callsBeforeRelease = repositories.Count;
        releaseFirst.SetResult();
        await preparation;
        Assert.Equal(1, callsBeforeRelease);
        Assert.False(overlap);
        Assert.Equal(["contributor/product", "owner/product"], repositories);
    }

    [Fact]
    public async Task RecordsMissingOptionalContextButRejectsAnUnclassifiedContextOnReuse()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.WriteGuidanceAsync("docs/CrossCuttingGuidance.md",
            "# Guidance\n[Orientation](Missing.md)\n## Overarching principles\n- A principle.\n## Topics\n### Topic\n- A rule.\n");
        var manifest = await fixture.PrepareAsync();
        var context = Assert.Single(manifest["context"]!.AsArray())!;
        Assert.Equal("missing", context["status"]!.GetValue<string>());
        Assert.Equal("ENOENT", context["reason"]!.GetValue<string>());
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());
        manifest["context"] = new JsonArray();
        await File.WriteAllTextAsync(Path.Combine(fixture.Output, "manifest.json"), JsonSerializer.Serialize(manifest), Utf8NoBom);
        await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
    }

    [Fact]
    public async Task RecordsAGuidanceSymlinkAsUnreadableContext()
    {
        await using var fixture = await Fixture.CreateAsync();
        var commit = fixture.Commit(new Dictionary<string, object>
        {
            [".github/skills/review-pull-request/routing.md"] =
                "| Changed path prefix | Guide |\n| --- | --- |\n| * | docs/CrossCuttingGuidance.md |\n",
            ["docs/CrossCuttingGuidance.md"] = "# Guidance\n[Architecture](Architecture.md)\n## Overarching principles\n- A principle.\n## Topics\n### Topic\n- A rule.\n",
            ["docs/Architecture.md"] = new FileEntry("120000", "Other.md"),
            [".github/copilot-instructions.md"] = "# Instructions\n## Security Concerns Are Out of Scope\nDo not review the excluded scope.\n",
        });
        var options = fixture.Options with { GuidanceRoot = null, Guidance = $"reviewer/guidance@{commit}" };
        var manifest = await fixture.PrepareAsync(options);
        var context = Assert.Single(manifest["context"]!.AsArray())!;
        Assert.Equal("unreadable", context["status"]!.GetValue<string>());
        Assert.Equal("symlink-text", context["reason"]!.GetValue<string>());
        Assert.True((await fixture.CheckAsync(options))["ready"]!.GetValue<bool>());
    }

    [Fact]
    public async Task RejectsAnOversizedSourceBody()
    {
        await using var fixture = await Fixture.CreateAsync();
        var commit = fixture.Commit(new Dictionary<string, object> { ["src/Oversized.cs"] = new string('x', 17 * 1024 * 1024) });
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PrepareReviewProgram.ExportTreeAsync(fixture.Repository, commit, Path.Combine(fixture.Root, "oversized")));
        Assert.Contains("exceeds 16 MiB", exception.Message);
    }

    [Fact]
    public async Task SupportsAnImmutableRemoteGuidanceSelection()
    {
        await using var fixture = await Fixture.CreateAsync();
        var commit = fixture.Commit(new Dictionary<string, object>
        {
            [".github/skills/review-pull-request/routing.md"] =
                "| Changed path prefix | Guide |\n| --- | --- |\n| * | docs/CrossCuttingGuidance.md |\n",
            ["docs/CrossCuttingGuidance.md"] = "# REMOTE_GUIDANCE\n[Architecture](Architecture.md)\n## Overarching principles\n- A rule.\n## Topics\n### Topic\n- Another rule.\n",
            ["docs/Architecture.md"] = "REMOTE_ARCHITECTURE\n",
            [".github/copilot-instructions.md"] = "# Instructions\n## Security Concerns Are Out of Scope\nDo not review the excluded scope.\n",
        });
        var options = fixture.Options with { GuidanceRoot = null, Guidance = $"reviewer/guidance@{commit}" };
        var manifest = await fixture.PrepareAsync(options);
        Assert.Equal("remote", manifest["guidance"]!["mode"]!.GetValue<string>());
        Assert.Contains("REMOTE_GUIDANCE", await File.ReadAllTextAsync(Path.Combine(fixture.Output,
            "guidance/docs/CrossCuttingGuidance.md.source"), Utf8NoBom));
        Assert.True((await fixture.CheckAsync(options))["ready"]!.GetValue<bool>());
    }

    public static TheoryData<string> ReuseMutations => new()
    {
        "changed head", "changed base-tip", "changed guidance", "changed source", "missing diff",
        "partial manifest", "missing maintained exclusion", "missing routed guide", "missing timings", "wrong source role",
    };

    [Theory]
    [MemberData(nameof(ReuseMutations))]
    public async Task RejectsReuseWithMutation(string mutation)
    {
        await using var fixture = await Fixture.CreateAsync();
        var manifest = await fixture.PrepareAsync();
        switch (mutation)
        {
            case "changed head": fixture.Pull["head"]!["sha"] = fixture.BaseTip; break;
            case "changed base-tip": fixture.BaseTip = fixture.MergeBase; break;
            case "changed guidance":
                await File.AppendAllTextAsync(Path.Combine(fixture.GuidanceRoot, "docs/CrossCuttingGuidance.md"), "\nCHANGED\n"); break;
            case "changed source":
                await File.AppendAllTextAsync(Path.Combine(fixture.Output, manifest["sources"]!["head"]!["root"]!.GetValue<string>(),
                    "src/Value.cs.source"), "MUTATED"); break;
            case "missing diff": File.Delete(Path.Combine(fixture.Output, "diff.patch")); break;
            case "partial manifest": manifest["sources"]!.AsObject().Remove("mergeBase"); await fixture.WriteManifestAsync(manifest); break;
            case "missing maintained exclusion": manifest["exclusions"] = new JsonArray(); await fixture.WriteManifestAsync(manifest); break;
            case "missing routed guide": manifest["guides"] = new JsonArray(); await fixture.WriteManifestAsync(manifest); break;
            case "missing timings": manifest["timingsMs"]!.AsObject().Remove("feedback"); await fixture.WriteManifestAsync(manifest); break;
            case "wrong source role":
                manifest["sources"]!["head"] = manifest["sources"]!["baseTip"]!.DeepClone(); await fixture.WriteManifestAsync(manifest); break;
        }
        await Assert.ThrowsAnyAsync<Exception>(fixture.CheckAsync);
    }

    [Fact]
    public async Task RejectsBundleWithLegacyJavaScriptProducerHash()
    {
        await using var fixture = await Fixture.CreateAsync();
        var manifest = await fixture.PrepareAsync();
        manifest["producer"] = "add2dd1e77ada0e14c7412983683ee88a6f008793f3b0a074f94da84039afda7";
        await fixture.WriteManifestAsync(manifest);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
        Assert.Contains("stale, mismatched, or from a different preparation version", exception.Message);
    }

    [Fact]
    public async Task CheckRejectsATargetThatMovesDuringValidation()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PrepareAsync();
        var moveOnCall = fixture.PullRequestCalls + 2;
        fixture.BeforePullResponse = call =>
        {
            if (call == moveOnCall)
            {
                fixture.MoveHeadToEquivalentCommit();
            }
        };
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
        Assert.Equal("The target or base branch moved during validation.", exception.Message);
    }

    [Fact]
    public async Task RetriesOneMovedTargetFromScratch()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.BeforePullResponse = call =>
        {
            if (call == 2)
            {
                File.WriteAllText(Path.Combine(fixture.Output, "partial.marker"), "partial", Utf8NoBom);
                fixture.MoveHeadToEquivalentCommit();
            }
        };
        var manifest = await fixture.PrepareAsync();
        Assert.Equal(4, fixture.PullRequestCalls);
        Assert.Equal(fixture.Head, manifest["target"]!["head"]!.GetValue<string>());
        Assert.False(File.Exists(Path.Combine(fixture.Output, "partial.marker")));
    }

    [Fact]
    public async Task BlocksWhenTheTargetMovesTwice()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.BeforePullResponse = call =>
        {
            if (call is 2 or 4)
            {
                fixture.MoveHeadToEquivalentCommit();
            }
        };
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.PrepareAsync);
        Assert.Equal("The target or base branch moved during preparation; no ready manifest was written.", exception.Message);
        Assert.Equal(4, fixture.PullRequestCalls);
        Assert.False(File.Exists(Path.Combine(fixture.Output, "manifest.json")));
    }

    [Fact]
    public void RejectsMalformedGuideTopicsAndUnresolvedPolicyAnchors()
    {
        foreach (var guide in new[]
        {
            "# Guidance\n## Topics\n### Topic\n- A rule.\n",
            "# Guidance\n## Overarching principles\n- A rule.\n## Topics\n### Topic\nNo bullets.\n",
            "# Guidance\n## Overarching principles\n- A rule.\n## Topics\n### Topic\n- A rule.\n### Topic\n- A rule.\n",
        })
        {
            Assert.Throws<InvalidOperationException>(() => PrepareReviewProgram.ValidateGuide(guide, "docs/Guide.md"));
        }
        Assert.Throws<InvalidOperationException>(() =>
            PrepareReviewProgram.ResolvePolicy("# Policy\n## Existing\n- A rule.\n", "missing", "docs/Policy.md"));
    }

    public static TheoryData<string> ReadinessFailures => new()
    {
        "truncated diff", "incomplete file list", "wrong file identity",
        "unavailable feedback", "unavailable Git fetch", "unavailable GitHub evidence",
    };

    [Theory]
    [MemberData(nameof(ReadinessFailures))]
    public async Task NeverWritesReadinessAfterFailure(string failure)
    {
        await using var fixture = await Fixture.CreateAsync();
        switch (failure)
        {
            case "truncated diff": fixture.Diff = []; break;
            case "incomplete file list": fixture.Files.RemoveAt(0); break;
            case "wrong file identity": fixture.Files[0]!["sha"] = new string('1', 40); break;
            case "unavailable feedback": fixture.FailReviews = true; break;
            case "unavailable Git fetch": fixture.FailFetch = true; break;
            case "unavailable GitHub evidence": fixture.FailApi = true; break;
        }
        await Assert.ThrowsAnyAsync<Exception>(fixture.PrepareAsync);
        Assert.False(File.Exists(Path.Combine(fixture.Output, "manifest.json")));
    }

    [Fact]
    public async Task RejectsAnInterruptedDirectoryAndAnExplicitWrongTargetHead()
    {
        await using var fixture = await Fixture.CreateAsync();
        Directory.CreateDirectory(fixture.Output);
        await Assert.ThrowsAnyAsync<Exception>(fixture.PrepareAsync);
        await Assert.ThrowsAnyAsync<Exception>(fixture.CheckAsync);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.PrepareAsync(fixture.Options with { Head = fixture.BaseTip }));
        Assert.Contains("expected frozen head", exception.Message);
    }

    [Fact]
    public void RejectsSuffixDirectoryCaseAndWindowsFilenameAliases()
    {
        foreach (var names in new[]
        {
            new[] { "x", "x.source/child" }, new[] { "x.source/child", "x" },
            new[] { "Path.cs", "path.cs" }, new[] { "src/CON.cs" }, new[] { "src/a:stream" }, new[] { "../escape" },
        })
        {
            Assert.Throws<InvalidOperationException>(() => PrepareReviewProgram.CheckPaths(names));
        }
        PrepareReviewProgram.CheckPaths(["src/File.cs", "src/AGENTS.md", ".github/copilot-instructions.md"]);
    }

    private static string SourcePath([CallerFilePath] string path = "") => path;

    private sealed record FileEntry(string Mode, string Body);

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(string root, bool components)
        {
            Root = root;
            Repository = Path.Combine(root, "repository");
            GuidanceRoot = Path.Combine(root, "guidance-checkout");
            Output = Path.Combine(root, "prepared");
            Directory.CreateDirectory(Repository);
            Git(Repository, "init", "--bare", "--quiet");
            var valuePath = components ? "src/Components/Value.cs" : "src/Value.cs";
            var original = new Dictionary<string, object>
            {
                [valuePath] = "MERGE_VALUE\n",
                ["src/Unchanged.cs"] = "MERGE_DEPENDENCY\n",
                ["src/OldName.cs"] = "RENAMED_BYTES\n",
                ["src/Deleted.cs"] = "DELETED_BYTES\n",
                ["src/Mode.cs"] = "REGULAR_BYTES\n",
                ["src/Large.cs"] = string.Concat(Enumerable.Repeat("unchanged padding\n", 2500)) + "RELEVANT_IMPLEMENTATION\n",
                ["AGENTS.md"] = "TARGET_INSTRUCTION_SENTINEL\n",
                [".github/copilot-instructions.md"] = "TARGET_ROOT_INSTRUCTION_SENTINEL\n",
                [".github/instructions/product.instructions.md"] = "TARGET_NESTED_INSTRUCTION_SENTINEL\n",
            };
            MergeBase = Commit(original);
            var headFiles = new Dictionary<string, object>(original)
            {
                [valuePath] = "HEAD_VALUE\n",
                ["src/Renamed.cs"] = original["src/OldName.cs"],
                ["src/Mode.cs"] = new FileEntry("120000", "Value.cs"),
            };
            headFiles.Remove("src/OldName.cs");
            headFiles.Remove("src/Deleted.cs");
            Head = Commit(headFiles, MergeBase);
            var baseFiles = new Dictionary<string, object>(original) { ["src/Unchanged.cs"] = "BASE_TIP_DEPENDENCY\n" };
            BaseTip = Commit(baseFiles, MergeBase);
            Directory.CreateDirectory(Path.Combine(GuidanceRoot, "docs"));
            File.WriteAllText(Path.Combine(GuidanceRoot, "docs/CrossCuttingGuidance.md"),
                "# Guidance\n## Overarching principles\n- ORIGINAL_GUIDANCE\n## Topics\n### Topic\n- Required clause.\n", Utf8NoBom);
            Directory.CreateDirectory(Path.Combine(GuidanceRoot, ".github"));
            Directory.CreateDirectory(Path.Combine(GuidanceRoot, ".github/skills/review-pull-request"));
            File.WriteAllText(Path.Combine(GuidanceRoot, ".github/skills/review-pull-request/routing.md"),
                "| Changed path prefix | Guide |\n| --- | --- |\n" +
                "| * | docs/CrossCuttingGuidance.md |\n| src/Components/ | docs/BlazorComponentsGuidance.md |\n", Utf8NoBom);
            File.WriteAllText(Path.Combine(GuidanceRoot, "docs/BlazorComponentsGuidance.md"),
                "# Components\n## Overarching principles\n- A principle.\n## Topics\n### Topic\n- A rule.\n", Utf8NoBom);
            File.WriteAllText(Path.Combine(GuidanceRoot, ".github/copilot-instructions.md"),
                "# Instructions\n## Security Concerns Are Out of Scope\nDo not review the excluded scope.\n", Utf8NoBom);
            Git(GuidanceRoot, "init", "--quiet");
            Git(GuidanceRoot, "add", ".");
            Git(GuidanceRoot, "commit", "--quiet", "-m", "Guidance");
            File.WriteAllText(Path.Combine(GuidanceRoot, "docs/CrossCuttingGuidance.md"),
                "# Guidance\n## Overarching principles\n- DIRTY_GUIDANCE\n## Topics\n### Topic\n- Required clause.\n", Utf8NoBom);
            Files = new JsonArray(
                FileJson(valuePath, "modified"),
                FileJson("src/OldName.cs", "removed"),
                FileJson("src/Renamed.cs", "added"),
                FileJson("src/Deleted.cs", "removed"),
                FileJson("src/Mode.cs", "modified"));
            Pull = new JsonObject
            {
                ["number"] = 42,
                ["state"] = "open",
                ["changed_files"] = Files.Count,
                ["head"] = new JsonObject { ["sha"] = Head, ["repo"] = new JsonObject { ["id"] = 2, ["full_name"] = "contributor/product" } },
                ["base"] = new JsonObject { ["ref"] = "release/test", ["repo"] = new JsonObject { ["id"] = 1, ["full_name"] = "owner/product" } },
            };
            Diff = GitBytes(Repository, "diff", "--binary", "--no-ext-diff", MergeBase, Head);
            Options = new("owner/product", "42", Output, GuidanceRoot: GuidanceRoot);

            JsonObject FileJson(string name, string status)
            {
                var side = status == "removed" ? MergeBase : Head;
                return new JsonObject { ["filename"] = name, ["status"] = status, ["sha"] = Git(Repository, "rev-parse", $"{side}:{name}") };
            }
        }

        public string Root { get; }
        public string Repository { get; }
        public string GuidanceRoot { get; }
        public string Output { get; }
        public string Head { get; private set; }
        public string MergeBase { get; set; }
        public string BaseTip { get; set; }
        public JsonObject Pull { get; }
        public JsonArray Files { get; set; }
        public byte[] Diff { get; set; }
        public PrepareReviewProgram.Options Options { get; }
        public bool FailReviews { get; set; }
        public bool FailFetch { get; set; }
        public bool FailApi { get; set; }
        public int PullRequestCalls { get; private set; }
        public Action<int>? BeforePullResponse { get; set; }

        public static Task<Fixture> CreateAsync(bool components = false)
        {
            Directory.CreateDirectory(TestArtifacts);
            var root = Path.Combine(TestArtifacts, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return Task.FromResult(new Fixture(root, components));
        }

        public string Commit(Dictionary<string, object> files, string? parent = null)
        {
            Git(Repository, "read-tree", "--empty");
            foreach (var pair in files)
            {
                var entry = pair.Value as FileEntry ?? new FileEntry("100644", (string)pair.Value);
                var sha = GitWithInput(Repository, Utf8NoBom.GetBytes(entry.Body), "hash-object", "-w", "--stdin");
                Git(Repository, "update-index", "--add", "--cacheinfo", $"{entry.Mode},{sha},{pair.Key}");
            }
            var arguments = new List<string> { "commit-tree", Git(Repository, "write-tree") };
            if (parent is not null) arguments.AddRange(["-p", parent]);
            arguments.AddRange(["-m", "Fixture"]);
            return Git(Repository, arguments.ToArray());
        }

        public Task<JsonObject> PrepareAsync() => PrepareAsync(Options);

        public Task<JsonObject> PrepareAsync(PrepareReviewProgram.Options options) =>
            PrepareReviewProgram.PrepareAsync(options, Dependencies());

        public Task<JsonObject> CheckAsync() => CheckAsync(Options);

        public Task<JsonObject> CheckAsync(PrepareReviewProgram.Options options) =>
            PrepareReviewProgram.PrepareAsync(options with { Check = true }, Dependencies());

        public async Task<string> SourceAsync(JsonObject manifest, string role, string name) =>
            await File.ReadAllTextAsync(Path.Combine(Output, manifest["sources"]![role]!["root"]!.GetValue<string>(),
                name.Replace('/', Path.DirectorySeparatorChar) + ".source"), Utf8NoBom);

        public async Task WriteGuidanceAsync(string name, string contents)
        {
            var path = Path.Combine(GuidanceRoot, name.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, contents, Utf8NoBom);
        }

        public Task WriteManifestAsync(JsonObject manifest) =>
            File.WriteAllTextAsync(Path.Combine(Output, "manifest.json"), JsonSerializer.Serialize(manifest), Utf8NoBom);

        public void MoveHeadToEquivalentCommit()
        {
            var arguments = new[]
            {
                "commit-tree",
                Git(Repository, "rev-parse", $"{Head}^{{tree}}"),
                "-p",
                Head,
                "-m",
                "Moved head",
            };
            Head = Git(Repository, arguments);
            Pull["head"]!["sha"] = Head;
            Diff = GitBytes(Repository, "diff", "--binary", "--no-ext-diff", MergeBase, Head);
        }

        public PrepareReviewProgram.Dependencies CreateDependencies() => new(ApiAsync, FetchAsync, ProducerSourcePath);

        private PrepareReviewProgram.Dependencies Dependencies() => CreateDependencies();

        private Task<JsonNode?> ApiAsync(string endpoint, string? accept)
        {
            if (FailApi) throw new InvalidOperationException("HTTP 503");
            if (endpoint == "repos/owner/product") return Node(new JsonObject { ["id"] = 1, ["full_name"] = "owner/product" });
            if (endpoint == "repos/reviewer/guidance") return Node(new JsonObject { ["id"] = 3, ["full_name"] = "reviewer/guidance" });
            if (accept is not null) return Node(JsonValue.Create(Convert.ToBase64String(Diff)));
            if (FailReviews && endpoint.Contains("/reviews?", StringComparison.Ordinal)) throw new InvalidOperationException("Review feedback unavailable");
            if (endpoint.Contains("/comments?", StringComparison.Ordinal) || endpoint.Contains("/reviews?", StringComparison.Ordinal)) return Node(new JsonArray());
            if (endpoint.Contains("/files?", StringComparison.Ordinal)) return Node(Files.DeepClone());
            if (endpoint.Contains("/git/ref/heads/", StringComparison.Ordinal)) return Node(new JsonObject { ["object"] = new JsonObject { ["sha"] = BaseTip } });
            if (endpoint.Contains("/compare/", StringComparison.Ordinal)) return Node(new JsonObject
            {
                ["base_commit"] = new JsonObject { ["sha"] = BaseTip },
                ["merge_base_commit"] = new JsonObject { ["sha"] = MergeBase },
            });
            if (endpoint.EndsWith("/pulls/42", StringComparison.Ordinal))
            {
                PullRequestCalls++;
                BeforePullResponse?.Invoke(PullRequestCalls);
                return Node(Pull.DeepClone());
            }
            throw new InvalidOperationException($"Unexpected API request: {endpoint}");

            static Task<JsonNode?> Node(JsonNode? node) => Task.FromResult(node);
        }

        private Task FetchAsync(string repo, IReadOnlyList<string> commits, string store)
        {
            if (FailFetch) throw new InvalidOperationException("Git fetch failed");
            Git(store, ["fetch", "--quiet", "--no-tags", Repository, .. commits]);
            return Task.CompletedTask;
        }

        public string Git(string root, params string[] arguments) =>
            Utf8NoBom.GetString(RunGit(root, arguments, null)).Trim();

        public byte[] GitBytes(string root, params string[] arguments) => RunGit(root, arguments, null);

        private string GitWithInput(string root, byte[] input, params string[] arguments) =>
            Utf8NoBom.GetString(RunGit(root, arguments, input)).Trim();

        private static byte[] RunGit(string root, IReadOnlyList<string> arguments, byte[]? input)
        {
            var location = root.EndsWith("guidance-checkout", StringComparison.Ordinal)
                ? new[] { "-C", root } : new[] { "--git-dir", root };
            var start = new ProcessStartInfo("git")
            {
                RedirectStandardInput = input is not null,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var pair in Identity) start.Environment[pair.Key] = pair.Value;
            foreach (var argument in new[] { "-c", "core.longpaths=true" }
                .Concat(location).Concat(["-c", "commit.gpgsign=false"]).Concat(arguments))
            {
                start.ArgumentList.Add(argument);
            }
            using var process = Process.Start(start)!;
            if (input is not null)
            {
                process.StandardInput.BaseStream.Write(input);
                process.StandardInput.Close();
            }
            using var output = new MemoryStream();
            process.StandardOutput.BaseStream.CopyTo(output);
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, error);
            return output.ToArray();
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(Root))
            {
                ClearReadOnlyAttributes(Root);
                Directory.Delete(Root, recursive: true);
            }
            return ValueTask.CompletedTask;
        }

        private static void ClearReadOnlyAttributes(string root)
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
            }
            File.SetAttributes(root, File.GetAttributes(root) & ~FileAttributes.ReadOnly);
        }
    }
}
