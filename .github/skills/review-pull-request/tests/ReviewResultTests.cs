// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json.Nodes;
using Xunit;

public class ReviewResultTests
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly string RepositoryRoot = Path.GetFullPath("../../../../", Path.GetDirectoryName(SourcePath())!);
    private static readonly string FixtureRoot = Path.Combine(
        RepositoryRoot, ".github/skills/review-pull-request/tests/fixtures/review-result");

    [Theory]
    [InlineData("findings", "### Review complete: 2 new source-supported findings")]
    [InlineData("no-findings", "### Review complete: no new source-supported findings; 1 unresolved")]
    [InlineData("incomplete", "### Review incomplete: One routed guide did not return a complete report")]
    [InlineData("blocked", "### Review blocked: The frozen merge-base source was unavailable")]
    public void RendersExactStatusSnapshots(string fixture, string heading)
    {
        var rendered = RenderFixture(fixture);

        Assert.StartsWith(heading + "\n", rendered.Markdown, StringComparison.Ordinal);
        Assert.Equal(
            File.ReadAllText(Path.Combine(FixtureRoot, fixture + ".md"), Utf8NoBom).ReplaceLineEndings("\n"),
            rendered.Markdown);
        Assert.Equal(1, rendered.CanonicalResult["schemaVersion"]!.GetValue<int>());
        Assert.Equal(1, rendered.CanonicalResult["publication"]!["summaryRendererVersion"]!.GetValue<int>());
        Assert.Equal(rendered.Digest, rendered.CanonicalResult["publication"]!["resultDigest"]!.GetValue<string>());
        Assert.DoesNotContain("\r", rendered.CanonicalJson, StringComparison.Ordinal);
    }

    [Fact]
    public void IdenticalValidatedEnvelopeProducesByteIdenticalOutput()
    {
        var bytes = File.ReadAllBytes(Fixture("findings.json"));

        var first = ReviewResultProgram.ValidateAndRender(bytes);
        var second = ReviewResultProgram.ValidateAndRender(bytes);

        Assert.Equal(Utf8NoBom.GetBytes(first.CanonicalJson), Utf8NoBom.GetBytes(second.CanonicalJson));
        Assert.Equal(Utf8NoBom.GetBytes(first.Markdown), Utf8NoBom.GetBytes(second.Markdown));
        Assert.Equal(first.Digest, second.Digest);
    }

    [Fact]
    public void CanonicalizesSemanticallyUnorderedArrays()
    {
        var first = FixtureNode("findings");
        var second = first.DeepClone().AsObject();
        Reverse(second["candidates"]!["accepted"]!.AsArray());
        Reverse(second["guides"]!.AsArray());
        Reverse(second["limitations"]!.AsArray());

        var firstRendered = Render(first);
        var secondRendered = Render(second);

        Assert.Equal(firstRendered.CanonicalJson, secondRendered.CanonicalJson);
        Assert.Equal(firstRendered.Markdown, secondRendered.Markdown);
        Assert.Equal(firstRendered.Digest, secondRendered.Digest);
    }

    [Theory]
    [InlineData("missing target", "target")]
    [InlineData("unknown root property", "properties must be exactly")]
    [InlineData("wrong schema version", "schema")]
    [InlineData("duplicate JSON property", "duplicate")]
    public void RejectsMalformedOrMissingFields(string mutation, string expected)
    {
        if (mutation == "duplicate JSON property")
        {
            var bytes = Utf8NoBom.GetBytes("""{"schemaVersion":1,"schemaVersion":1}""");
            var exception = Assert.Throws<InvalidOperationException>(() => ReviewResultProgram.ValidateAndRender(bytes));
            Assert.Contains(expected, exception.Message, StringComparison.OrdinalIgnoreCase);
            return;
        }

        var result = FixtureNode("no-findings");
        switch (mutation)
        {
            case "missing target":
                result.Remove("target");
                break;
            case "unknown root property":
                result["unknown"] = true;
                break;
            case "wrong schema version":
                result["schemaVersion"] = 2;
                break;
        }

        var error = Assert.Throws<InvalidOperationException>(() => Render(result));
        Assert.Contains(expected, error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsCandidateInMoreThanOneDisposition()
    {
        var result = FixtureNode("no-findings");
        result["candidates"]!["unresolved"]!.AsArray().Add(new JsonObject
        {
            ["id"] = "candidate-existing",
            ["summary"] = "The same candidate was placed twice",
            ["missingEvidence"] = "A single terminal disposition",
        });

        var exception = Assert.Throws<InvalidOperationException>(() => Render(result));
        Assert.Contains("more than one terminal disposition", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("accepted finding")]
    [InlineData("incomplete guide")]
    public void RejectsInvalidNoFindingsStatus(string mutation)
    {
        var result = FixtureNode("no-findings");
        if (mutation == "accepted finding")
        {
            result["candidates"]!["accepted"]!.AsArray().Add(
                FixtureNode("findings")["candidates"]!["accepted"]![0]!.DeepClone());
        }
        else
        {
            result["guides"]![0]!["status"] = "incomplete";
        }

        Assert.Throws<InvalidOperationException>(() => Render(result));
    }

    [Fact]
    public void IncompleteRetainsCandidatesButDoesNotRenderCompletedCoverage()
    {
        var rendered = RenderFixture("incomplete");

        Assert.Contains("not a completed review", rendered.Markdown, StringComparison.Ordinal);
        Assert.Contains("not a completed or publishable review", rendered.Markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("Review complete:", rendered.Markdown, StringComparison.Ordinal);
        Assert.False(rendered.CanonicalResult["publication"]!["eligible"]!.GetValue<bool>());
    }

    [Fact]
    public void CompletedResultCanRetainFullyExcludedGuideWithoutCallingItComplete()
    {
        var result = FixtureNode("no-findings");
        var guide = result["guides"]![0]!;
        guide["status"] = "excluded";
        guide["completedChecks"] = new JsonArray();

        var rendered = Render(result);

        Assert.Contains("1/2 guides completed; 1 fully excluded guide", rendered.Markdown, StringComparison.Ordinal);
        Assert.Contains("`docs/BlazorComponentsGuidance.md`: excluded", rendered.Markdown, StringComparison.Ordinal);
        Assert.Equal("NO_FINDINGS", rendered.CanonicalResult["status"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("source-only")]
    [InlineData("unavailable")]
    [InlineData("local-detached-worktree")]
    public void AcceptsExecutionClassifications(string classification)
    {
        var result = FixtureNode("no-findings");
        var execution = result["execution"]!.AsObject();
        switch (classification)
        {
            case "source-only":
                break;
            case "unavailable":
                execution["classification"] = "unavailable";
                execution["reason"] = "Optional execution was unavailable";
                break;
            case "local-detached-worktree":
                execution["classification"] = "local-detached-worktree";
                execution["available"] = true;
                execution["reason"] = null;
                execution["receipt"] = new JsonObject
                {
                    ["kind"] = "local",
                    ["status"] = "passed",
                    ["head"] = result["target"]!["head"]!.GetValue<string>(),
                    ["receiptId"] = "local-run-1",
                    ["url"] = null,
                    ["sha256"] = new string('e', 64),
                    ["testsPassed"] = 12,
                    ["testsFailed"] = 0,
                    ["testsSkipped"] = 1,
                };
                break;
        }

        var rendered = Render(result);
        Assert.Equal(classification, rendered.CanonicalResult["execution"]!["classification"]!.GetValue<string>());
    }

    [Fact]
    public void RetainsFailedLocalReproductionReceipt()
    {
        var result = FixtureNode("findings");
        result["execution"] = new JsonObject
        {
            ["classification"] = "local-detached-worktree",
            ["available"] = true,
            ["receipt"] = new JsonObject
            {
                ["kind"] = "local",
                ["status"] = "failed",
                ["head"] = result["target"]!["head"]!.GetValue<string>(),
                ["receiptId"] = "local-repro-1",
                ["url"] = null,
                ["sha256"] = new string('f', 64),
                ["testsPassed"] = 10,
                ["testsFailed"] = 1,
                ["testsSkipped"] = 0,
            },
            ["reason"] = null,
        };

        var rendered = Render(result);

        Assert.Contains("local-repro-1 (failed)", rendered.Markdown, StringComparison.Ordinal);
        Assert.Equal("failed", rendered.CanonicalResult["execution"]!["receipt"]!["status"]!.GetValue<string>());
    }

    [Fact]
    public void OptionalExecutionDoesNotBecomeRequiredForSourceOnlyCompletion()
    {
        var rendered = RenderFixture("no-findings");

        Assert.Equal("NO_FINDINGS", rendered.CanonicalResult["status"]!.GetValue<string>());
        Assert.Equal("source-only", rendered.CanonicalResult["execution"]!["classification"]!.GetValue<string>());
        Assert.Contains("No builds or tests run; source review only.", rendered.Markdown, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("@octocat")]
    [InlineData("/review now")]
    [InlineData("C:\\private\\bundle")]
    [InlineData("`C:\\private\\bundle`")]
    [InlineData("/private/bundle")]
    [InlineData("`/private/bundle`")]
    [InlineData("(/private/bundle)")]
    [InlineData("/.ssh/id_rsa")]
    [InlineData("https://example.invalid/value")]
    [InlineData("<script>value</script>")]
    [InlineData("```command```")]
    [InlineData("github_pat_secret")]
    [InlineData("value\u202Ehidden")]
    public void RejectsUnsafeOrPrivateText(string text)
    {
        var result = FixtureNode("no-findings");
        result["limitations"]![0] = text;

        Assert.Throws<InvalidOperationException>(() => Render(result));
    }

    [Fact]
    public void RejectsBackticksInRepositoryPaths()
    {
        var result = FixtureNode("findings");
        result["candidates"]!["accepted"]![0]!["anchor"]!["path"] = "src/a`spoofed.cs";

        Assert.Throws<InvalidOperationException>(() => Render(result));
    }

    [Fact]
    public void EscapesRequestedEffortInVisibleSummary()
    {
        var result = FixtureNode("no-findings");
        foreach (var guide in result["guides"]!.AsArray())
        {
            guide!["requested"]!["effort"] = "high_emphasis";
            guide["observed"]!["effort"] = "unknown";
        }

        var rendered = Render(result);

        Assert.Contains("effort(s) high\\_emphasis", rendered.Markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("effort(s) high_emphasis", rendered.Markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsOversizedText()
    {
        var result = FixtureNode("no-findings");
        result["limitations"]![0] = new string('a', 241);

        var exception = Assert.Throws<InvalidOperationException>(() => Render(result));
        Assert.Contains("1..240 characters", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BoundsRenderedCandidateDetails()
    {
        var result = FixtureNode("no-findings");
        var discarded = result["candidates"]!["discarded"]!.AsArray();
        for (var index = 0; index < 7; index++)
        {
            discarded.Add(new JsonObject
            {
                ["id"] = $"candidate-discarded-{index}",
                ["summary"] = $"Discarded candidate {index}",
                ["reason"] = "The frozen source disproves the claimed behavior",
            });
        }

        var rendered = Render(result);

        Assert.Contains("... and 2 more in `review-result.v1.json`", rendered.Markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("candidate-discarded-5", rendered.Markdown, StringComparison.Ordinal);
        Assert.Equal(7, rendered.CanonicalResult["candidates"]!["discarded"]!.AsArray().Count);
    }

    [Fact]
    public void RejectsKnownWorkerConfigurationMismatchOnCompleteGuide()
    {
        var result = FixtureNode("no-findings");
        result["guides"]![0]!["observed"]!["model"] = "gemini-4-argon";

        var exception = Assert.Throws<InvalidOperationException>(() => Render(result));
        Assert.Contains("configuration differ", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequiresUnevaluatedLabelForOtherRequestedModels()
    {
        var result = FixtureNode("no-findings");
        result["guides"]![0]!["requested"]!["model"] = "gemini-4-argon";
        result["guides"]![0]!["observed"]!["model"] = "unknown";

        var exception = Assert.Throws<InvalidOperationException>(() => Render(result));
        Assert.Contains("evaluationStatus must be UNEVALUATED", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidatesExistingFeedbackUrlsAgainstTargetRepository()
    {
        var result = FixtureNode("no-findings");
        result["candidates"]!["duplicate"]![0]!["existingFeedback"]!["url"] =
            "https://github.com/other/repository/pull/1";

        var exception = Assert.Throws<InvalidOperationException>(() => Render(result));
        Assert.Contains("target repository", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalOutputIsIdempotent()
    {
        var first = RenderFixture("findings");
        var second = ReviewResultProgram.ValidateAndRender(Utf8NoBom.GetBytes(first.CanonicalJson));

        Assert.Equal(first.CanonicalJson, second.CanonicalJson);
        Assert.Equal(first.Markdown, second.Markdown);
        Assert.Equal(first.Digest, second.Digest);
    }

    [Fact]
    public void RejectsResultDigestMismatch()
    {
        var canonical = JsonNode.Parse(RenderFixture("findings").CanonicalJson)!.AsObject();
        canonical["publication"]!["resultDigest"] = new string('0', 64);

        var exception = Assert.Throws<InvalidOperationException>(() => Render(canonical));
        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("FINDINGS", false)]
    [InlineData("NO_FINDINGS", false)]
    [InlineData("INCOMPLETE", true)]
    [InlineData("BLOCKED", true)]
    public void RejectsPublicationEligibilityMismatch(string status, bool eligible)
    {
        var fixture = status switch
        {
            "FINDINGS" => "findings",
            "NO_FINDINGS" => "no-findings",
            "INCOMPLETE" => "incomplete",
            "BLOCKED" => "blocked",
            _ => throw new InvalidOperationException(),
        };
        var result = FixtureNode(fixture);
        result["publication"]!["eligible"] = eligible;

        Assert.Throws<InvalidOperationException>(() => Render(result));
    }

    [Fact]
    public void LocalCliWritesArtifactsAndPrintsIdenticalMarkdown()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "review-result-tests", Guid.NewGuid().ToString("N"));
        var output = new StringWriter();
        var error = new StringWriter();
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = ReviewResultProgram.Run(["--input", Fixture("findings.json"), "--output", outputDirectory]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            var summary = File.ReadAllText(Path.Combine(outputDirectory, ReviewResultProgram.SummaryFileName), Utf8NoBom);
            Assert.Equal(summary, output.ToString());
            var canonical = File.ReadAllText(Path.Combine(outputDirectory, ReviewResultProgram.ResultFileName), Utf8NoBom);
            Assert.Equal(canonical, ReviewResultProgram.ValidateAndRender(Utf8NoBom.GetBytes(canonical)).CanonicalJson);
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static ReviewResultProgram.RenderedResult RenderFixture(string name) =>
        ReviewResultProgram.ValidateAndRender(File.ReadAllBytes(Fixture(name + ".json")));

    private static ReviewResultProgram.RenderedResult Render(JsonObject value) =>
        ReviewResultProgram.ValidateAndRender(Utf8NoBom.GetBytes(value.ToJsonString()));

    private static JsonObject FixtureNode(string name) =>
        JsonNode.Parse(File.ReadAllBytes(Fixture(name + ".json")))!.AsObject();

    private static string Fixture(string name) => Path.Combine(FixtureRoot, name);

    private static void Reverse(JsonArray values)
    {
        var reversed = values.Select(item => item!.DeepClone()).Reverse().ToArray();
        values.Clear();
        foreach (var item in reversed)
        {
            values.Add(item);
        }
    }

    private static string SourcePath([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
