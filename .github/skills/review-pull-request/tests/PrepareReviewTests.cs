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
    private const int MaximumNativeReadLineBytes = 2048;
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
        await AssertExecutionExclusionAsync(manifest, fixture);
        Assert.Contains("Do not review the excluded scope",
            await fixture.RequiredTextAsync(manifest["exclusions"]![1]!["body"]!));
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

    [Fact]
    public async Task ProjectsChangedFileMetadataWithoutDuplicatePatchesOrRestUrls()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Files[0]!["patch"] = new string('p', 5000);
        fixture.Files[0]!["blob_url"] = "https://example.invalid/blob";
        fixture.Files[0]!["raw_url"] = "https://example.invalid/raw";
        fixture.Files[0]!["contents_url"] = "https://example.invalid/contents";

        await fixture.PrepareAsync();

        var files = JsonNode.Parse(await File.ReadAllBytesAsync(Path.Combine(fixture.Output, "files.json")))!.AsArray();
        AssertJsonEqual(new JsonArray(
            ProjectedFile(fixture.Files[0]!, 11, 3, 14),
            ProjectedFile(fixture.Files[1]!, 0, 17, 17),
            ProjectedFile(fixture.Files[2]!, 23, 0, 23, "src/OldName.cs"),
            ProjectedFile(fixture.Files[3]!, 0, 29, 29),
            ProjectedFile(fixture.Files[4]!, 31, 37, 68)), files);
        Assert.DoesNotContain(files, item => item!.AsObject().ContainsKey("patch"));
        Assert.DoesNotContain(files, item => item!.AsObject().Any(pair => pair.Key.EndsWith("_url", StringComparison.Ordinal)));
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());

        static JsonObject ProjectedFile(
            JsonNode source, int additions, int deletions, int changes, string? previousFilename = null)
        {
            var result = new JsonObject
            {
                ["filename"] = source["filename"]!.GetValue<string>(),
                ["status"] = source["status"]!.GetValue<string>(),
                ["sha"] = source["sha"]!.GetValue<string>(),
                ["additions"] = additions,
                ["deletions"] = deletions,
                ["changes"] = changes,
            };
            if (previousFilename is not null)
            {
                result["previousFilename"] = previousFilename;
            }
            return result;
        }
    }

    [Fact]
    public async Task ProjectsPullMetadataAndExternalizesALargeMultilineBody()
    {
        await using var fixture = await Fixture.CreateAsync();
        var body = CreateLargeMultilineBody();
        Assert.Equal(24_931, Utf8NoBom.GetByteCount(body));
        Assert.Equal(178, body.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Equal(1045, body.Split('\n', StringSplitOptions.RemoveEmptyEntries).Max(line => Utf8NoBom.GetByteCount(line)));
        fixture.Pull["body"] = body;
        fixture.Pull["url"] = "https://api.example.invalid/pull";
        fixture.Pull["html_url"] = "https://example.invalid/pull";

        var manifest = await fixture.PrepareAsync();

        Assert.Equal(2, manifest["version"]!.GetValue<int>());
        var pull = JsonNode.Parse(await File.ReadAllBytesAsync(Path.Combine(fixture.Output, "pull.json")))!.AsObject();
        AssertJsonEqual(new JsonObject
        {
            ["number"] = 42,
            ["state"] = "open",
            ["title"] = "Fixture pull request",
            ["body"] = RequiredTextReference("required/pull/body.source", body),
            ["draft"] = false,
            ["author"] = new JsonObject
            {
                ["login"] = "contributor",
                ["id"] = 5_000_000_002L,
                ["type"] = "User",
            },
            ["labels"] = new JsonArray(
                new JsonObject { ["name"] = "area-test" },
                new JsonObject { ["name"] = "needs-attention" }),
            ["milestone"] = new JsonObject
            {
                ["number"] = 73,
                ["title"] = "Test milestone",
            },
            ["createdAt"] = "2026-10-25T10:02:03Z",
            ["updatedAt"] = "2026-10-26T11:12:13Z",
        }, pull);
        Assert.Equal(body, await fixture.RequiredTextAsync(pull["body"]!));
        Assert.DoesNotContain(pull, pair => pair.Key.EndsWith("url", StringComparison.OrdinalIgnoreCase));
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());
    }

    [Fact]
    public async Task ExternalizesPolicyExclusionAndFeedbackBodies()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.WriteGuidanceAsync("docs/Policy.md",
            "# Policy\n## Required clause\n- First line.\n- Second line.\n");
        await fixture.WriteGuidanceAsync("docs/CrossCuttingGuidance.md",
            "# Guidance\n## Overarching principles\n- Follow [the requirement](Policy.md#required-clause).\n" +
            "## Topics\n### Topic\n- Review changed code.\n");
        fixture.Comments.Add(new JsonObject
        {
            ["id"] = 5_000_000_101L,
            ["user"] = new JsonObject { ["login"] = "commenter", ["id"] = 5_000_000_201L, ["type"] = "User" },
            ["body"] = "Comment line 1\nComment line 2\n",
            ["created_at"] = "2026-10-25T10:03:04Z",
            ["updated_at"] = "2026-10-25T10:05:06Z",
            ["html_url"] = "https://example.invalid/comment",
        });
        fixture.Reviews.Add(new JsonObject
        {
            ["id"] = 5_000_000_202L,
            ["user"] = new JsonObject { ["login"] = "reviewer", ["id"] = 5_000_000_302L, ["type"] = "User" },
            ["body"] = "Review line 1\nReview line 2\n",
            ["state"] = "COMMENTED",
            ["submitted_at"] = "2026-10-25T12:13:14Z",
            ["commit_id"] = new string('a', 40),
            ["html_url"] = "https://example.invalid/review",
        });
        fixture.InlineComments.Add(new JsonObject
        {
            ["id"] = 5_000_000_303L,
            ["user"] = new JsonObject { ["login"] = "inline-bot", ["id"] = 5_000_000_403L, ["type"] = "Bot" },
            ["body"] = "Inline line 1\nInline line 2\n",
            ["path"] = "src/Value.cs",
            ["line"] = 41,
            ["side"] = "RIGHT",
            ["start_line"] = 37,
            ["start_side"] = "RIGHT",
            ["original_line"] = 39,
            ["original_start_line"] = 35,
            ["commit_id"] = new string('b', 40),
            ["original_commit_id"] = new string('c', 40),
            ["in_reply_to_id"] = 5_000_000_404L,
            ["created_at"] = "2026-10-25T14:15:16Z",
            ["updated_at"] = "2026-10-25T17:18:19Z",
            ["html_url"] = "https://example.invalid/inline",
        });

        var manifest = await fixture.PrepareAsync();

        AssertJsonEqual(RequiredTextReference("required/guides/0000/policies/0000.source",
            "## Required clause\n- First line.\n- Second line."), manifest["policies"]![0]!["body"]!);
        AssertJsonEqual(RequiredTextReference("required/coordinator/exclusions/0000-scope.source",
            "Running PR code, tests, CI, browser workflows, or implementation samples, except an optional minimal test or repro by a native local coordinator without a supplied bundle in a temporary detached worktree"),
            manifest["exclusions"]![0]!["scope"]!);
        Assert.Equal("## Required clause\n- First line.\n- Second line.",
            await fixture.RequiredTextAsync(manifest["policies"]![0]!["body"]!));
        Assert.Contains("Running PR code",
            await fixture.RequiredTextAsync(manifest["exclusions"]![0]!["scope"]!));
        Assert.Contains("Workers, hosted runs",
            await fixture.RequiredTextAsync(manifest["exclusions"]![0]!["reason"]!));
        Assert.Contains("Do not review the excluded scope",
            await fixture.RequiredTextAsync(manifest["exclusions"]![1]!["body"]!));
        var feedback = JsonNode.Parse(await File.ReadAllBytesAsync(Path.Combine(fixture.Output, "feedback.json")))!.AsObject();
        AssertJsonEqual(new JsonObject
        {
            ["comments"] = new JsonArray(new JsonObject
            {
                ["id"] = 5_000_000_101L,
                ["author"] = new JsonObject
                {
                    ["login"] = "commenter",
                    ["id"] = 5_000_000_201L,
                    ["type"] = "User",
                },
                ["body"] = RequiredTextReference(
                    "required/feedback/comments/5000000101.source", "Comment line 1\nComment line 2\n"),
                ["createdAt"] = "2026-10-25T10:03:04Z",
                ["updatedAt"] = "2026-10-25T10:05:06Z",
            }),
            ["reviews"] = new JsonArray(new JsonObject
            {
                ["id"] = 5_000_000_202L,
                ["author"] = new JsonObject
                {
                    ["login"] = "reviewer",
                    ["id"] = 5_000_000_302L,
                    ["type"] = "User",
                },
                ["body"] = RequiredTextReference(
                    "required/feedback/reviews/5000000202.source", "Review line 1\nReview line 2\n"),
                ["state"] = "COMMENTED",
                ["submittedAt"] = "2026-10-25T12:13:14Z",
                ["commitId"] = new string('a', 40),
            }),
            ["inline"] = new JsonArray(new JsonObject
            {
                ["id"] = 5_000_000_303L,
                ["author"] = new JsonObject
                {
                    ["login"] = "inline-bot",
                    ["id"] = 5_000_000_403L,
                    ["type"] = "Bot",
                },
                ["body"] = RequiredTextReference(
                    "required/feedback/inline/5000000303.source", "Inline line 1\nInline line 2\n"),
                ["path"] = "src/Value.cs",
                ["line"] = 41,
                ["side"] = "RIGHT",
                ["startLine"] = 37,
                ["startSide"] = "RIGHT",
                ["originalLine"] = 39,
                ["originalStartLine"] = 35,
                ["commitId"] = new string('b', 40),
                ["originalCommitId"] = new string('c', 40),
                ["inReplyToId"] = 5_000_000_404L,
                ["createdAt"] = "2026-10-25T14:15:16Z",
                ["updatedAt"] = "2026-10-25T17:18:19Z",
            }),
        }, feedback);
        Assert.Equal("Comment line 1\nComment line 2\n",
            await fixture.RequiredTextAsync(feedback["comments"]![0]!["body"]!));
        Assert.Equal("Review line 1\nReview line 2\n",
            await fixture.RequiredTextAsync(feedback["reviews"]![0]!["body"]!));
        Assert.Equal("Inline line 1\nInline line 2\n",
            await fixture.RequiredTextAsync(feedback["inline"]![0]!["body"]!));
        Assert.All(feedback.SelectMany(group => group.Value!.AsArray()).Select(item => item!.AsObject()),
            item => Assert.DoesNotContain(item, pair => pair.Key.EndsWith("url", StringComparison.OrdinalIgnoreCase)));
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());
    }

    public static TheoryData<string> RequiredTextMutations => new()
    {
        "missing",
        "tampered",
        "wrong length",
        "wrong hash",
        "path escape",
        "reused",
        "orphan",
    };

    [Theory]
    [MemberData(nameof(RequiredTextMutations))]
    public async Task RejectsInvalidExternalRequiredText(string mutation)
    {
        await using var fixture = await Fixture.CreateAsync();
        var manifest = await fixture.PrepareAsync();
        var reference = manifest["exclusions"]![0]!["reason"]!.AsObject();
        var path = Path.Combine(fixture.Output,
            reference["path"]!.GetValue<string>().Replace('/', Path.DirectorySeparatorChar));
        switch (mutation)
        {
            case "missing":
                File.Delete(path);
                break;
            case "tampered":
                await File.AppendAllTextAsync(path, "tampered", Utf8NoBom);
                break;
            case "wrong length":
                reference["utf8Bytes"] = reference["utf8Bytes"]!.GetValue<int>() + 1;
                await fixture.WriteManifestAsync(manifest);
                break;
            case "wrong hash":
                reference["sha256"] = new string('0', 64);
                await fixture.WriteManifestAsync(manifest);
                break;
            case "path escape":
                reference["path"] = "required/../escape.source";
                await fixture.WriteManifestAsync(manifest);
                break;
            case "reused":
                manifest["exclusions"]![0]!["scope"] = reference.DeepClone();
                await fixture.WriteManifestAsync(manifest);
                break;
            case "orphan":
                await fixture.AddRequiredTextAsync(manifest, "required/orphan.source", "orphan\n");
                await fixture.WriteManifestAsync(manifest);
                break;
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
        if (mutation == "reused")
        {
            Assert.Equal(
                "Prepared required text path is reused: required/coordinator/exclusions/0000-reason.source",
                exception.Message);
        }
        else if (mutation == "orphan")
        {
            Assert.Equal("Prepared required text references are incomplete.", exception.Message);
        }
        else
        {
            Assert.Contains("required text", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData(MaximumNativeReadLineBytes, true)]
    [InlineData(MaximumNativeReadLineBytes + 1, false)]
    public async Task EnforcesRequiredTextSingleLineNativeReadBound(int lineBytes, bool accepted)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Pull["body"] = new string('x', lineBytes) + "\n";

        if (accepted)
        {
            var manifest = await fixture.PrepareAsync();
            var pull = JsonNode.Parse(await File.ReadAllBytesAsync(Path.Combine(fixture.Output, "pull.json")))!.AsObject();
            Assert.Equal(new string('x', lineBytes) + "\n", await fixture.RequiredTextAsync(pull["body"]!));
            Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());
            Assert.True(manifest["ready"]!.GetValue<bool>());
        }
        else
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.PrepareAsync);
            Assert.Equal("Required text line exceeds 2048 UTF-8 bytes: required/pull/body.source:1", exception.Message);
            Assert.False(File.Exists(Path.Combine(fixture.Output, "manifest.json")));
        }
    }

    [Fact]
    public async Task RejectsAnOversizedValidManifestOnReuse()
    {
        await using var fixture = await Fixture.CreateAsync();
        var manifest = await fixture.PrepareAsync();
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());
        var compact = JsonSerializer.Serialize(manifest);
        Assert.True(Utf8NoBom.GetByteCount(compact) > MaximumNativeReadLineBytes);
        await File.WriteAllTextAsync(Path.Combine(fixture.Output, "manifest.json"), compact, Utf8NoBom);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
        Assert.Equal("Prepared serialized JSON line exceeds 2048 UTF-8 bytes: manifest.json:1", exception.Message);
    }

    [Fact]
    public async Task RejectsAnOversizedMalformedManifestLineBeforeParsing()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PrepareAsync();
        await File.WriteAllTextAsync(Path.Combine(fixture.Output, "manifest.json"),
            new string('x', MaximumNativeReadLineBytes + 1), Utf8NoBom);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
        Assert.Equal("Prepared serialized JSON line exceeds 2048 UTF-8 bytes: manifest.json:1", exception.Message);
    }

    [Fact]
    public async Task ReusesTheSameBundleWithAndWithoutATrailingOutputSeparator()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PrepareAsync();
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());

        var options = fixture.Options with { Output = fixture.Output + Path.DirectorySeparatorChar };
        Assert.True((await fixture.CheckAsync(options))["ready"]!.GetValue<bool>());
    }

    public static TheoryData<string> InlineRequiredTextMutations => new()
    {
        "pull body",
        "feedback body",
        "milestone",
    };

    [Theory]
    [MemberData(nameof(InlineRequiredTextMutations))]
    public async Task RejectsNonObjectProjectedReferencesOnReuse(string mutation)
    {
        await using var fixture = await Fixture.CreateAsync();
        if (mutation == "feedback body")
        {
            fixture.Comments.Add(new JsonObject
            {
                ["id"] = 101,
                ["user"] = new JsonObject { ["login"] = "commenter", ["id"] = 201, ["type"] = "User" },
                ["body"] = "Comment body.\n",
                ["created_at"] = "2026-10-25T10:00:00Z",
                ["updated_at"] = "2026-10-25T10:01:00Z",
            });
        }
        var manifest = await fixture.PrepareAsync();
        if (mutation is "pull body" or "milestone")
        {
            var pull = JsonNode.Parse(await File.ReadAllBytesAsync(Path.Combine(fixture.Output, "pull.json")))!.AsObject();
            if (mutation == "pull body")
            {
                var reference = pull["body"]!;
                pull["body"] = "inline";
                await fixture.RemoveRequiredTextAsync(manifest, reference);
            }
            else
            {
                pull["milestone"] = "inline";
            }
            await fixture.WriteJsonArtifactAsync(manifest, "pull.json", pull);
        }
        else
        {
            var feedback = JsonNode.Parse(await File.ReadAllBytesAsync(Path.Combine(fixture.Output, "feedback.json")))!.AsObject();
            var reference = feedback["comments"]![0]!["body"]!;
            feedback["comments"]![0]!["body"] = "inline";
            await fixture.RemoveRequiredTextAsync(manifest, reference);
            await fixture.WriteJsonArtifactAsync(manifest, "feedback.json", feedback);
        }
        await fixture.WriteManifestAsync(manifest);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
        Assert.Contains("malformed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task KeepsEverySerializedJsonLineWithinTheNativeReadBound()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Pull["body"] = CreateLargeMultilineBody();
        fixture.Files[0]!["patch"] = new string('p', 5000);
        await fixture.WriteGuidanceAsync("docs/Policy.md",
            "# Policy\n## Required clause\n" + string.Concat(Enumerable.Repeat("- Required policy line.\n", 150)));
        await fixture.WriteGuidanceAsync("docs/CrossCuttingGuidance.md",
            "# Guidance\n## Overarching principles\n- Follow [the requirement](Policy.md#required-clause).\n" +
            "## Topics\n### Topic\n- Review changed code.\n");

        await fixture.PrepareAsync();

        foreach (var name in new[] { "manifest.json", "pull.json", "files.json", "feedback.json" })
        {
            var lines = await File.ReadAllLinesAsync(Path.Combine(fixture.Output, name), Utf8NoBom);
            Assert.All(lines, line => Assert.True(Utf8NoBom.GetByteCount(line) <= MaximumNativeReadLineBytes,
                $"{name} contains a {Utf8NoBom.GetByteCount(line)}-byte serialized line."));
        }
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
    public async Task KeepsCallerFrozenBaseWhenTheBranchAdvances()
    {
        await using var fixture = await Fixture.CreateAsync();
        var baseTip = fixture.BaseTip;
        var options = fixture.Options with
        {
            Head = fixture.Head,
            BaseTip = baseTip,
            MergeBase = fixture.MergeBase,
            BaseRef = fixture.Pull["base"]!["ref"]!.GetValue<string>(),
        };
        fixture.BaseTip = fixture.Commit(new() { ["src/Unchanged.cs"] = "ADVANCED_BRANCH\n" }, baseTip);

        var manifest = await fixture.PrepareAsync(options);

        Assert.Equal(baseTip, manifest["target"]!["baseTip"]!.GetValue<string>());
        Assert.Equal(fixture.MergeBase, manifest["target"]!["mergeBase"]!.GetValue<string>());
        Assert.Equal("BASE_TIP_DEPENDENCY\n", await fixture.SourceAsync(manifest, "baseTip", "src/Unchanged.cs"));
        Assert.Equal(0, fixture.BranchRefCalls);
    }

    [Fact]
    public async Task RejectsARetargetedPullRequestWithCallerFrozenBase()
    {
        await using var fixture = await Fixture.CreateAsync();
        var options = fixture.Options with
        {
            Head = fixture.Head,
            BaseTip = fixture.BaseTip,
            MergeBase = fixture.MergeBase,
            BaseRef = fixture.Pull["base"]!["ref"]!.GetValue<string>(),
        };
        fixture.Pull["base"]!["ref"] = "release/different";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.PrepareAsync(options));

        Assert.Equal("The live PR base ref differs from the expected frozen base.", exception.Message);
        Assert.False(File.Exists(Path.Combine(fixture.Output, "manifest.json")));
    }

    [Fact]
    public async Task RejectsADifferentCallerFrozenMergeBase()
    {
        await using var fixture = await Fixture.CreateAsync();
        var options = fixture.Options with
        {
            Head = fixture.Head,
            BaseTip = fixture.BaseTip,
            MergeBase = fixture.BaseTip,
            BaseRef = fixture.Pull["base"]!["ref"]!.GetValue<string>(),
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.PrepareAsync(options));

        Assert.Equal("The comparison differs from the expected frozen merge base.", exception.Message);
        Assert.False(File.Exists(Path.Combine(fixture.Output, "manifest.json")));
    }

    [Fact]
    public async Task AcceptsCallerFrozenBaseArgumentsAtTheCommandLine()
    {
        await using var fixture = await Fixture.CreateAsync();
        var exitCode = await PrepareReviewProgram.RunAsync([
            "--repo", "owner/product", "--pr", "42", "--head", fixture.Head,
            "--base-tip", fixture.BaseTip, "--merge-base", fixture.MergeBase,
            "--base-ref", fixture.Pull["base"]!["ref"]!.GetValue<string>(),
            "--guidance-root", fixture.GuidanceRoot, "--output", fixture.Output,
        ], fixture.CreateDependencies());

        Assert.Equal(0, exitCode);
        var manifest = JsonNode.Parse(await File.ReadAllBytesAsync(Path.Combine(fixture.Output, "manifest.json")))!;
        Assert.Equal(fixture.BaseTip, manifest["target"]!["baseTip"]!.GetValue<string>());
        Assert.Equal(fixture.MergeBase, manifest["target"]!["mergeBase"]!.GetValue<string>());
        Assert.Equal(0, fixture.BranchRefCalls);
    }

    [Theory]
    [InlineData("head")]
    [InlineData("base-tip")]
    [InlineData("merge-base")]
    [InlineData("base-ref")]
    public async Task RejectsIncompleteCallerFrozenBase(string missing)
    {
        await using var fixture = await Fixture.CreateAsync();
        var options = fixture.Options with
        {
            Head = missing == "head" ? null : fixture.Head,
            BaseTip = missing == "base-tip" ? null : fixture.BaseTip,
            MergeBase = missing == "merge-base" ? null : fixture.MergeBase,
            BaseRef = missing == "base-ref" ? null : fixture.Pull["base"]!["ref"]!.GetValue<string>(),
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.PrepareAsync(options));

        Assert.Contains("A frozen base requires", exception.Message);
        Assert.Equal(0, fixture.PullRequestCalls);
        Assert.False(File.Exists(Path.Combine(fixture.Output, "manifest.json")));
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
        Assert.Equal("## Required clause\n- Only this requirement.",
            await fixture.RequiredTextAsync(policy["body"]!));
        manifest["policies"] = new JsonArray();
        await File.WriteAllTextAsync(Path.Combine(fixture.Output, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions) + "\n", Utf8NoBom);
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
        Assert.Equal($"## {heading}\n- Validate the behavior.",
            await fixture.RequiredTextAsync(policy["body"]!));
        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());

        var policyBody = manifest["policies"]![0]!["body"]!;
        manifest["policies"] = new JsonArray();
        await fixture.RemoveRequiredTextAsync(manifest, policyBody);
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
            ["additions"] = 0,
            ["deletions"] = 0,
            ["changes"] = 0,
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
        changed[0]!.AsObject().Remove("previousFilename");
        var bytes = Utf8NoBom.GetBytes(JsonSerializer.Serialize(changed, JsonOptions) + "\n");
        await File.WriteAllBytesAsync(filename, bytes);
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        manifest["artifacts"]!["files.json"] = hash;
        foreach (var guide in manifest["guides"]!.AsArray())
        {
            var input = guide!["assignment"]!["files"]!.AsArray()
                .Single(file => file!["kind"]!.GetValue<string>() == "changedFiles")!;
            input["utf8Bytes"] = bytes.Length;
            input["sha256"] = hash;
        }
        await File.WriteAllTextAsync(Path.Combine(fixture.Output, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions) + "\n", Utf8NoBom);
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
    public async Task ProjectsDisjointPerGuideWorkerInputsWithTrustedMetadata()
    {
        await using var fixture = await Fixture.CreateAsync(components: true);
        await ConfigureWorkerIsolationGuidanceAsync(fixture);
        var manifest = await fixture.PrepareAsync();
        var guides = manifest["guides"]!.AsArray();

        Assert.Equal(2, manifest["version"]!.GetValue<int>());
        Assert.Equal(2, guides.Count);
        var requiredPaths = new HashSet<string>(StringComparer.Ordinal);
        for (var guideIndex = 0; guideIndex < guides.Count; guideIndex++)
        {
            var guide = guides[guideIndex]!.AsObject();
            Assert.Equal(["path", "topics", "assignment"], guide.Select(property => property.Key));
            var guidePath = guide["path"]!.GetValue<string>();
            var assignment = guide["assignment"]!.AsObject();
            Assert.Equal(["head", "mergeBase", "baseTip"],
                assignment["sourceRoles"]!.AsArray().Select(role => role!.GetValue<string>()));

            var files = assignment["files"]!.AsArray().Select(file => file!.AsObject()).ToArray();
            var expectedPolicyPath = $"required/guides/{guideIndex:D4}/policies/0000.source";
            var expectedExclusionPaths = new[]
            {
                $"required/guides/{guideIndex:D4}/exclusions/0000-scope.source",
                $"required/guides/{guideIndex:D4}/exclusions/0000-reason.source",
                $"required/guides/{guideIndex:D4}/exclusions/0001-body.source",
            };
            Assert.Equal(new[]
            {
                ("guide", $"guidance/{guidePath}.source"),
                ("diff", "diff.patch"),
                ("changedFiles", "files.json"),
                ("policy", expectedPolicyPath),
                ("exclusion", expectedExclusionPaths[0]),
                ("exclusion", expectedExclusionPaths[1]),
                ("exclusion", expectedExclusionPaths[2]),
            }, files.Select(file => (
                file["kind"]!.GetValue<string>(),
                file["path"]!.GetValue<string>())));

            foreach (var file in files)
            {
                var path = file["path"]!.GetValue<string>();
                var bytes = await File.ReadAllBytesAsync(Path.Combine(
                    fixture.Output, path.Replace('/', Path.DirectorySeparatorChar)));
                Assert.Equal(bytes.Length, file["utf8Bytes"]!.GetValue<int>());
                Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), file["sha256"]!.GetValue<string>());
            }

            var policyPaths = files
                .Where(file => file["kind"]!.GetValue<string>() == "policy")
                .Select(file => file["path"]!.GetValue<string>())
                .ToArray();
            Assert.Equal([expectedPolicyPath], policyPaths);
            Assert.Equal([expectedPolicyPath],
                manifest["policies"]!.AsArray()
                    .Where(policy => policy!["guide"]!.GetValue<string>() == guidePath)
                    .Select(policy => policy!["body"]!["path"]!.GetValue<string>()));
            Assert.True(requiredPaths.Add(expectedPolicyPath));

            var exclusionPaths = files
                .Where(file => file["kind"]!.GetValue<string>() == "exclusion")
                .Select(file => file["path"]!.GetValue<string>())
                .ToArray();
            Assert.Equal(expectedExclusionPaths, exclusionPaths);
            Assert.All(exclusionPaths, path => Assert.True(requiredPaths.Add(path)));
        }

        Assert.True((await fixture.CheckAsync())["ready"]!.GetValue<bool>());

        var skill = Regex.Replace(await File.ReadAllTextAsync(Path.Combine(
            RepositoryRoot, ".github/skills/review-pull-request/SKILL.md"), Utf8NoBom), @"\s+", " ");
        Assert.Contains(
            "Workers treat this metadata as producer/checker-validated trust data and must not recompute either value.",
            skill, StringComparison.Ordinal);
        Assert.Contains("Read each directly to EOF with native `view` or `read_file`", skill, StringComparison.Ordinal);
        Assert.Contains(
            "Page with `view_range` or the equivalent `read_file` offset and limit until the final line; a truncated read is a required-input failure, not completion.",
            skill, StringComparison.Ordinal);
        Assert.Contains("Workers must not invoke `bash`, `shell`, or `powershell`.", skill, StringComparison.Ordinal);
        Assert.Contains(
            "Native `rg`, `grep`, or `glob` is allowed only beneath an assigned frozen `source/<sha>` root",
            skill, StringComparison.Ordinal);
        Assert.DoesNotContain("verify the assigned UTF-8 byte length and SHA-256", skill, StringComparison.Ordinal);
        Assert.Contains(
            "For a native local coordinator shell denial, do not propose `--allow-all-paths`.",
            skill, StringComparison.Ordinal);
        var workerContractStart = skill.IndexOf("PATH: per-guide-worker", StringComparison.Ordinal);
        Assert.True(workerContractStart >= 0);
        var workerContract = skill[workerContractStart..];
        Assert.Contains(
            "must read every assigned non-source file to EOF through native `view` or `read_file`, using `view_range` or the equivalent `read_file` offset and limit until the final line.",
            workerContract, StringComparison.Ordinal);
        Assert.Contains(
            "It must not invoke `bash`, `shell`, or `powershell`, and may use native `rg`, `grep`, or `glob` only beneath its assigned frozen source roots.",
            workerContract, StringComparison.Ordinal);

        var workflow = Regex.Replace(await File.ReadAllTextAsync(Path.Combine(
            RepositoryRoot, ".github/workflows/pull-request-review.md"), Utf8NoBom), @"\s+", " ");
        Assert.Contains(
            "Treat the assignment metadata as producer/checker-validated trust data; do not ask workers to recompute UTF-8 lengths or SHA-256 hashes.",
            workflow, StringComparison.Ordinal);
        Assert.Contains(
            "Workers must read each assigned non-source file to EOF through native `view` or `read_file` calls",
            workflow, StringComparison.Ordinal);
        Assert.Contains(
            "They must page with `view_range` or the equivalent `read_file` offset and limit until the final line; a truncated read is a required-input failure, not completion.",
            workflow, StringComparison.Ordinal);
        Assert.Contains("must not invoke `bash`, `shell`, or `powershell`", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "may use native `rg`, `grep`, or `glob` only beneath their assigned frozen source roots",
            workflow, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("path")]
    [InlineData("length")]
    [InlineData("hash")]
    [InlineData("policy")]
    [InlineData("exclusion")]
    [InlineData("extra")]
    [InlineData("missing")]
    [InlineData("source roles")]
    [InlineData("tampered")]
    public async Task CheckRejectsChangedWorkerAssignment(string mutation)
    {
        await using var fixture = await Fixture.CreateAsync(components: true);
        await ConfigureWorkerIsolationGuidanceAsync(fixture);
        var manifest = await fixture.PrepareAsync();
        var firstGuide = manifest["guides"]![0]!.AsObject();
        var secondGuide = manifest["guides"]![1]!.AsObject();
        var files = firstGuide["assignment"]!["files"]!.AsArray();
        var guideInput = files.Single(file => file!["kind"]!.GetValue<string>() == "guide")!.AsObject();

        switch (mutation)
        {
            case "path":
                guideInput["path"] = secondGuide["assignment"]!["files"]!.AsArray()
                    .Single(file => file!["kind"]!.GetValue<string>() == "guide")!["path"]!.GetValue<string>();
                break;
            case "length":
                guideInput["utf8Bytes"] = guideInput["utf8Bytes"]!.GetValue<int>() + 1;
                break;
            case "hash":
                guideInput["sha256"] = new string('0', 64);
                break;
            case "policy":
                var policyIndex = files
                    .Select((file, index) => (File: file!, Index: index))
                    .Single(item => item.File["kind"]!.GetValue<string>() == "policy")
                    .Index;
                files[policyIndex] = secondGuide["assignment"]!["files"]!.AsArray()
                    .Single(file => file!["kind"]!.GetValue<string>() == "policy")!.DeepClone();
                break;
            case "exclusion":
                var exclusionIndex = files
                    .Select((file, index) => (File: file!, Index: index))
                    .First(item => item.File["kind"]!.GetValue<string>() == "exclusion")
                    .Index;
                files[exclusionIndex] = secondGuide["assignment"]!["files"]!.AsArray()
                    .First(file => file!["kind"]!.GetValue<string>() == "exclusion")!.DeepClone();
                break;
            case "extra":
                files.Add(guideInput.DeepClone());
                break;
            case "missing":
                files.Remove(files.Single(file => file!["kind"]!.GetValue<string>() == "changedFiles"));
                break;
            case "source roles":
                firstGuide["assignment"]!["sourceRoles"] = new JsonArray("head", "baseTip", "mergeBase");
                break;
            case "tampered":
                var projected = files.First(file => file!["kind"]!.GetValue<string>() == "exclusion")!;
                await File.AppendAllTextAsync(Path.Combine(fixture.Output,
                    projected["path"]!.GetValue<string>().Replace('/', Path.DirectorySeparatorChar)), "tampered", Utf8NoBom);
                break;
        }

        await fixture.WriteManifestAsync(manifest);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
        Assert.Contains(mutation == "tampered" ? "required text" : "worker assignment",
            exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ConfigureWorkerIsolationGuidanceAsync(Fixture fixture)
    {
        await fixture.WriteGuidanceAsync(".github/skills/review-pull-request/routing.md",
            "| Changed path prefix | Guide |\n| --- | --- |\n" +
            "| * | docs/CrossCuttingGuidance.md |\n" +
            "| src/Components/ | docs/BlazorComponentsGuidance.md |\n");
        await fixture.WriteGuidanceAsync("docs/CrossCuttingGuidance.md",
            "# Cross-cutting\n## Overarching principles\n- Apply [the cross policy](CrossPolicy.md#cross-policy).\n" +
            "## Topics\n### Correctness\n- Review correctness.\n");
        await fixture.WriteGuidanceAsync("docs/BlazorComponentsGuidance.md",
            "# Components\n## Overarching principles\n- Apply [the Components policy](ComponentsPolicy.md#components-policy).\n" +
            "## Topics\n### Components\n- Review Components behavior.\n");
        await fixture.WriteGuidanceAsync("docs/CrossPolicy.md",
            "# Cross requirements\n## Cross Policy\n- Cross-cutting requirement.\n");
        await fixture.WriteGuidanceAsync("docs/ComponentsPolicy.md",
            "# Components requirements\n## Components Policy\n- Components requirement.\n");
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
        await File.WriteAllTextAsync(Path.Combine(fixture.Output, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions) + "\n", Utf8NoBom);
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
        await AssertExecutionExclusionAsync(manifest, fixture);
        Assert.True((await fixture.CheckAsync(options))["ready"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData("scope")]
    [InlineData("reason")]
    [InlineData("legacy")]
    public async Task RejectsReuseWithChangedExecutionExclusion(string mutation)
    {
        await using var fixture = await Fixture.CreateAsync();
        var manifest = await fixture.PrepareAsync();
        var exclusion = manifest["exclusions"]![0]!;
        if (mutation == "legacy")
        {
            await fixture.ReplaceRequiredTextAsync(manifest, exclusion["scope"]!,
                "Running PR code, tests, CI, browser workflows, or implementation samples");
            await fixture.ReplaceRequiredTextAsync(manifest, exclusion["reason"]!,
                "This is a source-only review; assess changed tests and contracts from source.");
        }
        else
        {
            await fixture.ReplaceRequiredTextAsync(manifest, exclusion[mutation]!, "CHANGED");
        }
        await fixture.WriteManifestAsync(manifest);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.CheckAsync);
        Assert.Equal("Prepared exclusions do not match the trusted instruction snapshot.", exception.Message);
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

    private static async Task AssertExecutionExclusionAsync(JsonObject manifest, Fixture fixture)
    {
        Assert.Equal("Running PR code, tests, CI, browser workflows, or implementation samples, except an optional minimal test or repro by a native local coordinator without a supplied bundle in a temporary detached worktree",
            await fixture.RequiredTextAsync(manifest["exclusions"]![0]!["scope"]!));
        Assert.Equal("Workers, hosted runs, and supplied-bundle runs remain source-only; assess changed tests and contracts from source. " +
            "Only a native local coordinator without a supplied bundle may optionally run a minimal test or repro under SKILL.md's detached-worktree validation rules: " +
            "use a temporary detached worktree at manifest.target.head, never execute target code in the developer checkout or bundle, record the exact command and result, " +
            "and remove the temporary worktree and scratch files afterwards. If optional validation is unavailable, record the reason and remain source-only without reporting INCOMPLETE.",
            await fixture.RequiredTextAsync(manifest["exclusions"]![0]!["reason"]!));
    }

    private static string CreateLargeMultilineBody() =>
        new string('b', 1045) + "\n"
        + string.Concat(Enumerable.Repeat(new string('b', 134) + "\n", 167))
        + string.Concat(Enumerable.Repeat(new string('b', 133) + "\n", 10));

    private static JsonObject RequiredTextReference(string path, string text)
    {
        var bytes = Utf8NoBom.GetBytes(text);
        return new JsonObject
        {
            ["path"] = path,
            ["utf8Bytes"] = bytes.Length,
            ["sha256"] = Convert.ToHexStringLower(SHA256.HashData(bytes)),
        };
    }

    private static void AssertJsonEqual(JsonNode expected, JsonNode actual) =>
        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Expected:{Environment.NewLine}{expected.ToJsonString(JsonOptions)}{Environment.NewLine}" +
            $"Actual:{Environment.NewLine}{actual.ToJsonString(JsonOptions)}");

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
                FileJson(valuePath, "modified", 11, 3, 14),
                FileJson("src/OldName.cs", "removed", 0, 17, 17),
                FileJson("src/Renamed.cs", "added", 23, 0, 23, "src/OldName.cs"),
                FileJson("src/Deleted.cs", "removed", 0, 29, 29),
                FileJson("src/Mode.cs", "modified", 31, 37, 68));
            Pull = new JsonObject
            {
                ["number"] = 42,
                ["state"] = "open",
                ["title"] = "Fixture pull request",
                ["body"] = "Fixture body.\n",
                ["draft"] = false,
                ["user"] = new JsonObject { ["login"] = "contributor", ["id"] = 5_000_000_002L, ["type"] = "User" },
                ["labels"] = new JsonArray(
                    new JsonObject { ["name"] = "area-test", ["color"] = "ffffff" },
                    new JsonObject { ["name"] = "needs-attention", ["color"] = "123456" }),
                ["milestone"] = new JsonObject { ["number"] = 73, ["title"] = "Test milestone" },
                ["created_at"] = "2026-10-25T10:02:03Z",
                ["updated_at"] = "2026-10-26T11:12:13Z",
                ["changed_files"] = Files.Count,
                ["head"] = new JsonObject { ["sha"] = Head, ["repo"] = new JsonObject { ["id"] = 2, ["full_name"] = "contributor/product" } },
                ["base"] = new JsonObject { ["ref"] = "release/test", ["repo"] = new JsonObject { ["id"] = 1, ["full_name"] = "owner/product" } },
            };
            Diff = GitBytes(Repository, "diff", "--binary", "--no-ext-diff", MergeBase, Head);
            Options = new("owner/product", "42", Output, GuidanceRoot: GuidanceRoot);

            JsonObject FileJson(
                string name, string status, int additions, int deletions, int changes, string? previousFilename = null)
            {
                var side = status == "removed" ? MergeBase : Head;
                var result = new JsonObject
                {
                    ["filename"] = name,
                    ["status"] = status,
                    ["sha"] = Git(Repository, "rev-parse", $"{side}:{name}"),
                    ["additions"] = additions,
                    ["deletions"] = deletions,
                    ["changes"] = changes,
                };
                if (previousFilename is not null)
                {
                    result["previous_filename"] = previousFilename;
                }
                return result;
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
        public JsonArray Comments { get; } = [];
        public JsonArray Reviews { get; } = [];
        public JsonArray InlineComments { get; } = [];
        public byte[] Diff { get; set; }
        public PrepareReviewProgram.Options Options { get; }
        public bool FailReviews { get; set; }
        public bool FailFetch { get; set; }
        public bool FailApi { get; set; }
        public int PullRequestCalls { get; private set; }
        public int BranchRefCalls { get; private set; }
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

        public async Task<string> RequiredTextAsync(JsonNode reference) =>
            await File.ReadAllTextAsync(Path.Combine(Output,
                reference["path"]!.GetValue<string>().Replace('/', Path.DirectorySeparatorChar)), Utf8NoBom);

        public async Task WriteGuidanceAsync(string name, string contents)
        {
            var path = Path.Combine(GuidanceRoot, name.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, contents, Utf8NoBom);
        }

        public Task WriteManifestAsync(JsonObject manifest) =>
            File.WriteAllTextAsync(Path.Combine(Output, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions) + "\n", Utf8NoBom);

        public async Task WriteJsonArtifactAsync(JsonObject manifest, string name, JsonNode value)
        {
            var bytes = Utf8NoBom.GetBytes(JsonSerializer.Serialize(value, JsonOptions) + "\n");
            await File.WriteAllBytesAsync(Path.Combine(Output, name), bytes);
            manifest["artifacts"]![name] = Convert.ToHexStringLower(SHA256.HashData(bytes));
        }

        public async Task ReplaceRequiredTextAsync(JsonObject manifest, JsonNode reference, string text)
        {
            var bytes = Utf8NoBom.GetBytes(text);
            var path = Path.Combine(Output,
                reference["path"]!.GetValue<string>().Replace('/', Path.DirectorySeparatorChar));
            await File.WriteAllBytesAsync(path, bytes);
            reference["utf8Bytes"] = bytes.Length;
            reference["sha256"] = Convert.ToHexStringLower(SHA256.HashData(bytes));
            await RefreshRequiredTextDigestAsync(manifest);
        }

        public async Task RemoveRequiredTextAsync(JsonObject manifest, JsonNode reference)
        {
            File.Delete(Path.Combine(Output,
                reference["path"]!.GetValue<string>().Replace('/', Path.DirectorySeparatorChar)));
            await RefreshRequiredTextDigestAsync(manifest);
        }

        public async Task AddRequiredTextAsync(JsonObject manifest, string name, string text)
        {
            var path = Path.Combine(Output, name.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, text, Utf8NoBom);
            await RefreshRequiredTextDigestAsync(manifest);
        }

        private async Task RefreshRequiredTextDigestAsync(JsonObject manifest)
        {
            var root = Path.Combine(Output, manifest["requiredText"]!["root"]!.GetValue<string>());
            var names = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
                .Order(StringComparer.Ordinal)
                .ToArray();
            using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var name in names)
            {
                var bytes = await File.ReadAllBytesAsync(Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar)));
                digest.AppendData(Utf8NoBom.GetBytes(
                    $"{name}\0{Convert.ToHexStringLower(SHA256.HashData(bytes))}\n"));
            }
            manifest["requiredText"]!["files"] = names.Length;
            manifest["requiredText"]!["sha256"] = Convert.ToHexStringLower(digest.GetHashAndReset());
        }

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
            if (endpoint.Contains("/issues/42/comments?", StringComparison.Ordinal)) return Node(Comments.DeepClone());
            if (endpoint.Contains("/pulls/42/reviews?", StringComparison.Ordinal)) return Node(Reviews.DeepClone());
            if (endpoint.Contains("/pulls/42/comments?", StringComparison.Ordinal)) return Node(InlineComments.DeepClone());
            if (endpoint.Contains("/files?", StringComparison.Ordinal)) return Node(Files.DeepClone());
            if (endpoint.Contains("/git/ref/heads/", StringComparison.Ordinal))
            {
                BranchRefCalls++;
                return Node(new JsonObject { ["object"] = new JsonObject { ["sha"] = BaseTip } });
            }
            if (endpoint.Contains("/compare/", StringComparison.Ordinal))
            {
                var comparisonBase = endpoint.Split("/compare/", StringSplitOptions.None)[1].Split("...", StringSplitOptions.None)[0];
                return Node(new JsonObject
                {
                    ["base_commit"] = new JsonObject { ["sha"] = comparisonBase },
                    ["merge_base_commit"] = new JsonObject { ["sha"] = MergeBase },
                });
            }
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
