// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

#if !PREPARE_REVIEW_TESTS
return ReviewResultProgram.Run(args);
#endif

internal static class ReviewResultProgram
{
    internal const int SchemaVersion = 1;
    internal const int RendererVersion = 1;
    internal const string ResultFileName = "review-result.v1.json";
    internal const string SummaryFileName = "review-summary.v1.md";

    private const int MaximumInputBytes = 1024 * 1024;
    private const int MaximumSummaryBytes = 16 * 1024;
    private const int MaximumGuides = 16;
    private const int MaximumCandidatesPerDisposition = 50;
    private const int MaximumListItems = 64;
    private const int MaximumRenderedItems = 5;
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly Regex Sha40Regex = new("^[a-f0-9]{40}$", RegexOptions.CultureInvariant);
    private static readonly Regex Sha256Regex = new("^[a-f0-9]{64}$", RegexOptions.CultureInvariant);
    private static readonly Regex IdentifierRegex = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant);
    private static readonly Regex RepositoryRegex = new("^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$", RegexOptions.CultureInvariant);
    private static readonly Regex HostRegex = new("^[A-Za-z0-9.-]+$", RegexOptions.CultureInvariant);
    private static readonly Regex MentionRegex = new(@"(^|[^A-Za-z0-9])@[A-Za-z0-9]", RegexOptions.CultureInvariant);
    private static readonly Regex SlashCommandRegex = new(@"(^|\s)/[A-Za-z]", RegexOptions.CultureInvariant);
    private static readonly Regex WindowsPathRegex = new(@"(^|[^A-Za-z0-9])[A-Za-z]:[\\/]", RegexOptions.CultureInvariant);
    private static readonly Regex PosixPathRegex = new(
        @"(^|[^A-Za-z0-9])/(?:[A-Za-z0-9._-]+/)+[A-Za-z0-9._-]+",
        RegexOptions.CultureInvariant);
    private static readonly Regex SecretRegex = new(
        @"(?i)(github_pat_|gh[pousr]_[A-Za-z0-9]|AKIA[0-9A-Z]{12}|Bearer\s+[A-Za-z0-9._-])",
        RegexOptions.CultureInvariant);

    internal sealed record RenderedResult(JsonObject CanonicalResult, string CanonicalJson, string Markdown, string Digest);

    private sealed record Target(string Host, string Repository, int PullRequest, string Head, string MergeBase, string BaseTip);
    private sealed record Guidance(string Source, string RoutingSha256);
    private sealed record AgentConfiguration(string AgentType, string Model, string Effort);
    private sealed record Exclusion(string Scope, string Reason);
    private sealed record Guide(
        string Path,
        string Status,
        string EvaluationStatus,
        AgentConfiguration Requested,
        AgentConfiguration Observed,
        IReadOnlyList<string> CompletedChecks,
        IReadOnlyList<Exclusion> Exclusions);
    private sealed record Uncovered(string Area, string Reason);
    private sealed record Anchor(string Path, int Line, int? EndLine);
    private sealed record AcceptedCandidate(
        string Id,
        string Severity,
        int Confidence,
        string Summary,
        Anchor Anchor,
        string Trigger,
        string Effect);
    private sealed record FeedbackIdentity(string Kind, string Id, string Status, string? ReviewedHead, string? Url);
    private sealed record DuplicateCandidate(
        string Id,
        string Summary,
        string CanonicalRootCause,
        FeedbackIdentity ExistingFeedback);
    private sealed record UnresolvedCandidate(string Id, string Summary, string MissingEvidence);
    private sealed record DiscardedCandidate(string Id, string Summary, string Reason);
    private sealed record CandidateLedger(
        IReadOnlyList<AcceptedCandidate> Accepted,
        IReadOnlyList<DuplicateCandidate> Duplicate,
        IReadOnlyList<UnresolvedCandidate> Unresolved,
        IReadOnlyList<DiscardedCandidate> Discarded);
    private sealed record ExecutionReceipt(
        string Kind,
        string Status,
        string Head,
        string ReceiptId,
        string? Url,
        string Sha256,
        int? TestsPassed,
        int? TestsFailed,
        int? TestsSkipped);
    private sealed record Execution(string Classification, bool Available, ExecutionReceipt? Receipt, string? Reason);
    private sealed record Publication(bool Eligible, string? ResultDigest, int SummaryRendererVersion);
    private sealed record ReviewResult(
        string Status,
        string? Reason,
        Target Target,
        Guidance Guidance,
        string ReviewPath,
        IReadOnlyList<Guide> Guides,
        IReadOnlyList<Uncovered> Uncovered,
        CandidateLedger Candidates,
        Execution Execution,
        IReadOnlyList<string> Limitations,
        Publication Publication);

    internal static int Run(string[] args)
    {
        string? outputDirectory = null;
        try
        {
            var (input, output) = ParseArguments(args);
            outputDirectory = Path.GetFullPath(output);
            var bytes = File.ReadAllBytes(input);
            var rendered = ValidateAndRender(bytes);

            Directory.CreateDirectory(outputDirectory);
            WriteAtomically(Path.Combine(outputDirectory, ResultFileName), rendered.CanonicalJson);
            WriteAtomically(Path.Combine(outputDirectory, SummaryFileName), rendered.Markdown);
            Console.Out.Write(rendered.Markdown);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"BLOCKED: {error.Message}");
            return 1;
        }
    }

    internal static RenderedResult ValidateAndRender(byte[] bytes)
    {
        Require(bytes.Length is > 0 and <= MaximumInputBytes, $"Review result must be 1..{MaximumInputBytes} UTF-8 bytes.");
        RejectDuplicateProperties(bytes);

        JsonNode root;
        try
        {
            root = JsonNode.Parse(bytes) ?? throw new InvalidOperationException("Review result is empty.");
        }
        catch (JsonException error)
        {
            throw new InvalidOperationException($"Review result JSON is malformed: {error.Message}", error);
        }

        var result = ParseResult(AsObject(root, "review result"));
        var canonicalWithoutDigest = BuildCanonicalJson(result, resultDigest: null);
        var digest = Convert.ToHexStringLower(SHA256.HashData(Utf8NoBom.GetBytes(Serialize(canonicalWithoutDigest))));
        if (!string.IsNullOrEmpty(result.Publication.ResultDigest))
        {
            Require(result.Publication.ResultDigest == digest, "publication.resultDigest does not match the canonical review result.");
        }

        var canonical = BuildCanonicalJson(result, digest);
        var canonicalJson = Serialize(canonical);
        var markdown = Render(result, digest);
        Require(Utf8NoBom.GetByteCount(markdown) <= MaximumSummaryBytes,
            $"Rendered review summary exceeds {MaximumSummaryBytes} UTF-8 bytes.");
        return new(canonical, canonicalJson, markdown, digest);
    }

    internal static string Serialize(JsonNode value) =>
        value.ToJsonString(new JsonSerializerOptions { WriteIndented = true, NewLine = "\n" }) + "\n";

    private static (string Input, string Output) ParseArguments(string[] args)
    {
        string? input = null;
        string? output = null;
        for (var index = 0; index < args.Length; index++)
        {
            Require(args[index] is "--input" or "--output" && index + 1 < args.Length,
                $"Unknown or incomplete option: {args[index]}");
            var value = args[++index];
            if (args[index - 1] == "--input")
            {
                input = value;
            }
            else
            {
                output = value;
            }
        }

        Require(!string.IsNullOrWhiteSpace(input), "Specify --input <review-result.json>.");
        Require(!string.IsNullOrWhiteSpace(output), "Specify --output <artifact-directory>.");
        return (Path.GetFullPath(input!), output!);
    }

    private static ReviewResult ParseResult(JsonObject root)
    {
        RequireProperties(root, "review result",
        [
            "schemaVersion", "status", "reason", "target", "guidance", "review", "guides",
            "uncovered", "candidates", "execution", "limitations", "publication",
        ]);
        Require(Integer(root, "schemaVersion", "review result") == SchemaVersion,
            $"schemaVersion must be {SchemaVersion}.");

        var status = EnumString(root, "status", "review result", "FINDINGS", "NO_FINDINGS", "INCOMPLETE", "BLOCKED");
        var reason = OptionalSafeString(root, "reason", "review result", 240);
        var target = ParseTarget(Object(root, "target", "review result"));
        var guidance = ParseGuidance(Object(root, "guidance", "review result"));

        var review = Object(root, "review", "review result");
        RequireProperties(review, "review", ["path"]);
        var reviewPath = EnumString(review, "path", "review", "per-guide", "single-reviewer");

        var guides = ParseGuides(Array(root, "guides", "review result"));
        var uncovered = ParseUncovered(Array(root, "uncovered", "review result"));
        var candidates = ParseCandidates(Object(root, "candidates", "review result"), target);
        var execution = ParseExecution(Object(root, "execution", "review result"), target);
        var limitations = SafeStringArray(Array(root, "limitations", "review result"), "limitations", MaximumListItems, 240);
        var publication = ParsePublication(Object(root, "publication", "review result"));

        ValidateStatus(status, reason, guides, candidates, publication);
        return new(status, reason, target, guidance, reviewPath, guides, uncovered, candidates, execution, limitations, publication);
    }

    private static Target ParseTarget(JsonObject value)
    {
        RequireProperties(value, "target", ["host", "repository", "pullRequest", "head", "mergeBase", "baseTip"]);
        var host = SafeString(value, "host", "target", 120);
        Require(HostRegex.IsMatch(host), "target.host is invalid.");
        var repository = SafeString(value, "repository", "target", 200);
        Require(RepositoryRegex.IsMatch(repository), "target.repository must be OWNER/REPOSITORY.");
        var pullRequest = Integer(value, "pullRequest", "target");
        Require(pullRequest > 0, "target.pullRequest must be positive.");
        return new(
            host,
            repository,
            pullRequest,
            Sha40(value, "head", "target"),
            Sha40(value, "mergeBase", "target"),
            Sha40(value, "baseTip", "target"));
    }

    private static Guidance ParseGuidance(JsonObject value)
    {
        RequireProperties(value, "guidance", ["source", "routingSha256"]);
        return new(
            SafeString(value, "source", "guidance", 160),
            Sha256(value, "routingSha256", "guidance"));
    }

    private static IReadOnlyList<Guide> ParseGuides(JsonArray values)
    {
        Require(values.Count <= MaximumGuides, $"guides must contain at most {MaximumGuides} entries.");
        var guides = new List<Guide>(values.Count);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (node, index) in values.Select((node, index) => (node, index)))
        {
            var value = AsObject(node, $"guides[{index}]");
            RequireProperties(value, $"guides[{index}]",
            [
                "path", "status", "evaluationStatus", "requested", "observed", "completedChecks", "exclusions",
            ]);
            var path = RelativePath(value, "path", $"guides[{index}]");
            Require(paths.Add(path), $"Duplicate guide path: {path}");
            var status = EnumString(value, "status", $"guides[{index}]", "complete", "incomplete", "excluded");
            var evaluation = EnumString(value, "evaluationStatus", $"guides[{index}]", "EVALUATED", "UNEVALUATED");
            var requested = ParseAgentConfiguration(Object(value, "requested", $"guides[{index}]"), $"guides[{index}].requested");
            var observed = ParseAgentConfiguration(Object(value, "observed", $"guides[{index}]"), $"guides[{index}].observed");
            var completedChecks = SafeStringArray(
                Array(value, "completedChecks", $"guides[{index}]"),
                $"guides[{index}].completedChecks",
                MaximumListItems,
                160);
            var exclusions = ParseExclusions(Array(value, "exclusions", $"guides[{index}]"), $"guides[{index}].exclusions");
            Require(status != "excluded" || (completedChecks.Count == 0 && exclusions.Count > 0),
                $"{path} must have no completed checks and at least one exclusion when status is excluded.");

            var mismatch = KnownMismatch(requested.AgentType, observed.AgentType)
                || KnownMismatch(requested.Model, observed.Model)
                || KnownEffortMismatch(requested.Effort, observed.Effort);
            Require(status == "incomplete" || !mismatch,
                $"{path} cannot be complete because requested and observed worker configuration differ.");
            Require(requested.AgentType == "general-purpose" || status == "incomplete",
                $"{path} cannot be complete because the requested agent type is not general-purpose.");
            var expectedEvaluation = requested.Model == "gpt-5.6-sol" ? "EVALUATED" : "UNEVALUATED";
            Require(evaluation == expectedEvaluation,
                $"{path} evaluationStatus must be {expectedEvaluation} for requested model {requested.Model}.");
            guides.Add(new(path, status, evaluation, requested, observed, completedChecks, exclusions));
        }

        return guides.OrderBy(guide => guide.Path, StringComparer.Ordinal).ToArray();
    }

    private static AgentConfiguration ParseAgentConfiguration(JsonObject value, string description)
    {
        RequireProperties(value, description, ["agentType", "model", "effort"]);
        return new(
            SafeIdentifierOrUnknown(value, "agentType", description),
            SafeIdentifierOrUnknown(value, "model", description),
            SafeString(value, "effort", description, 80));
    }

    private static IReadOnlyList<Exclusion> ParseExclusions(JsonArray values, string description)
    {
        Require(values.Count <= MaximumListItems, $"{description} must contain at most {MaximumListItems} entries.");
        return values.Select((node, index) =>
        {
            var value = AsObject(node, $"{description}[{index}]");
            RequireProperties(value, $"{description}[{index}]", ["scope", "reason"]);
            return new Exclusion(
                SafeString(value, "scope", $"{description}[{index}]", 200),
                SafeString(value, "reason", $"{description}[{index}]", 240));
        })
        .OrderBy(exclusion => exclusion.Scope, StringComparer.Ordinal)
        .ThenBy(exclusion => exclusion.Reason, StringComparer.Ordinal)
        .ToArray();
    }

    private static IReadOnlyList<Uncovered> ParseUncovered(JsonArray values)
    {
        Require(values.Count <= MaximumListItems, $"uncovered must contain at most {MaximumListItems} entries.");
        return values.Select((node, index) =>
        {
            var value = AsObject(node, $"uncovered[{index}]");
            RequireProperties(value, $"uncovered[{index}]", ["area", "reason"]);
            return new Uncovered(
                SafeString(value, "area", $"uncovered[{index}]", 160),
                SafeString(value, "reason", $"uncovered[{index}]", 240));
        })
        .OrderBy(item => item.Area, StringComparer.Ordinal)
        .ThenBy(item => item.Reason, StringComparer.Ordinal)
        .ToArray();
    }

    private static CandidateLedger ParseCandidates(JsonObject value, Target target)
    {
        RequireProperties(value, "candidates", ["accepted", "duplicate", "unresolved", "discarded"]);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var accepted = ParseCandidateArray(value, "accepted", ParseAccepted, ids, target);
        var duplicate = ParseCandidateArray(value, "duplicate", ParseDuplicate, ids, target);
        var unresolved = ParseCandidateArray(value, "unresolved", ParseUnresolved, ids, target);
        var discarded = ParseCandidateArray(value, "discarded", ParseDiscarded, ids, target);

        return new(
            accepted.OrderBy(candidate => SeverityRank(candidate.Severity))
                .ThenByDescending(candidate => candidate.Confidence)
                .ThenBy(candidate => candidate.Anchor.Path, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Anchor.Line)
                .ThenBy(candidate => candidate.Id, StringComparer.Ordinal)
                .ToArray(),
            duplicate.OrderBy(candidate => candidate.Id, StringComparer.Ordinal).ToArray(),
            unresolved.OrderBy(candidate => candidate.Id, StringComparer.Ordinal).ToArray(),
            discarded.OrderBy(candidate => candidate.Id, StringComparer.Ordinal).ToArray());
    }

    private static List<T> ParseCandidateArray<T>(
        JsonObject ledger,
        string name,
        Func<JsonObject, string, Target, T> parser,
        HashSet<string> ids,
        Target target)
    {
        var values = Array(ledger, name, "candidates");
        Require(values.Count <= MaximumCandidatesPerDisposition,
            $"candidates.{name} must contain at most {MaximumCandidatesPerDisposition} entries.");
        var candidates = new List<T>(values.Count);
        foreach (var (node, index) in values.Select((node, index) => (node, index)))
        {
            var description = $"candidates.{name}[{index}]";
            var value = AsObject(node, description);
            var id = SafeIdentifier(value, "id", description);
            Require(ids.Add(id), $"Candidate {id} has more than one terminal disposition.");
            candidates.Add(parser(value, description, target));
        }

        return candidates;
    }

    private static AcceptedCandidate ParseAccepted(JsonObject value, string description, Target target)
    {
        RequireProperties(value, description, ["id", "severity", "confidence", "summary", "anchor", "trigger", "effect"]);
        var anchor = Object(value, "anchor", description);
        RequireProperties(anchor, $"{description}.anchor", ["path", "line", "endLine", "side"]);
        Require(EnumString(anchor, "side", $"{description}.anchor", "RIGHT") == "RIGHT",
            $"{description}.anchor.side must be RIGHT.");
        var line = Integer(anchor, "line", $"{description}.anchor");
        var endLine = OptionalInteger(anchor, "endLine", $"{description}.anchor");
        Require(line > 0 && (endLine is null || endLine >= line), $"{description}.anchor line range is invalid.");
        var confidence = Integer(value, "confidence", description);
        Require(confidence is >= 0 and <= 10, $"{description}.confidence must be between 0 and 10.");
        return new(
            SafeIdentifier(value, "id", description),
            EnumString(value, "severity", description, "P1", "P2", "P3"),
            confidence,
            SafeString(value, "summary", description, 200),
            new(RelativePath(anchor, "path", $"{description}.anchor"), line, endLine),
            SafeString(value, "trigger", description, 240),
            SafeString(value, "effect", description, 320));
    }

    private static DuplicateCandidate ParseDuplicate(JsonObject value, string description, Target target)
    {
        RequireProperties(value, description, ["id", "summary", "canonicalRootCause", "existingFeedback"]);
        var feedback = Object(value, "existingFeedback", description);
        RequireProperties(feedback, $"{description}.existingFeedback", ["kind", "id", "status", "reviewedHead", "url"]);
        var reviewedHead = OptionalString(feedback, "reviewedHead", $"{description}.existingFeedback");
        if (reviewedHead is not null)
        {
            Require(Sha40Regex.IsMatch(reviewedHead), $"{description}.existingFeedback.reviewedHead must be a 40-character lowercase SHA.");
        }
        var url = OptionalUrl(feedback, "url", $"{description}.existingFeedback", target);
        return new(
            SafeIdentifier(value, "id", description),
            SafeString(value, "summary", description, 200),
            SafeIdentifier(value, "canonicalRootCause", description),
            new(
                EnumString(feedback, "kind", $"{description}.existingFeedback", "review", "review-comment", "issue-comment"),
                SafeIdentifier(feedback, "id", $"{description}.existingFeedback"),
                EnumString(feedback, "status", $"{description}.existingFeedback", "open", "resolved", "outdated"),
                reviewedHead,
                url));
    }

    private static UnresolvedCandidate ParseUnresolved(JsonObject value, string description, Target target)
    {
        RequireProperties(value, description, ["id", "summary", "missingEvidence"]);
        return new(
            SafeIdentifier(value, "id", description),
            SafeString(value, "summary", description, 200),
            SafeString(value, "missingEvidence", description, 320));
    }

    private static DiscardedCandidate ParseDiscarded(JsonObject value, string description, Target target)
    {
        RequireProperties(value, description, ["id", "summary", "reason"]);
        return new(
            SafeIdentifier(value, "id", description),
            SafeString(value, "summary", description, 200),
            SafeString(value, "reason", description, 320));
    }

    private static Execution ParseExecution(JsonObject value, Target target)
    {
        RequireProperties(value, "execution", ["classification", "available", "receipt", "reason"]);
        var classification = EnumString(
            value,
            "classification",
            "execution",
            "source-only",
            "local-detached-worktree",
            "unavailable");
        var available = Boolean(value, "available", "execution");
        var reason = OptionalSafeString(value, "reason", "execution", 240);
        ExecutionReceipt? receipt = null;
        if (value["receipt"] is not null)
        {
            var receiptValue = Object(value, "receipt", "execution");
            RequireProperties(receiptValue, "execution.receipt",
            [
                "kind", "status", "head", "receiptId", "url", "sha256", "testsPassed", "testsFailed", "testsSkipped",
            ]);
            var head = Sha40(receiptValue, "head", "execution.receipt");
            Require(head == target.Head, "execution.receipt.head must match target.head.");
            receipt = new(
                EnumString(receiptValue, "kind", "execution.receipt", "local", "workflow"),
                EnumString(receiptValue, "status", "execution.receipt", "passed", "failed"),
                head,
                SafeIdentifier(receiptValue, "receiptId", "execution.receipt"),
                OptionalUrl(receiptValue, "url", "execution.receipt", target),
                Sha256(receiptValue, "sha256", "execution.receipt"),
                OptionalNonNegativeInteger(receiptValue, "testsPassed", "execution.receipt"),
                OptionalNonNegativeInteger(receiptValue, "testsFailed", "execution.receipt"),
                OptionalNonNegativeInteger(receiptValue, "testsSkipped", "execution.receipt"));
        }

        switch (classification)
        {
            case "source-only":
                Require(!available && receipt is null && reason is null,
                    "source-only execution must be unavailable without a receipt or reason.");
                break;
            case "local-detached-worktree":
                Require(available && receipt is not null && receipt.Kind == "local" && reason is null,
                    "local-detached-worktree execution requires an available local receipt.");
                if (receipt!.TestsFailed is not null)
                {
                    Require(receipt.Status == "passed" && receipt.TestsFailed == 0
                        || receipt.Status == "failed" && receipt.TestsFailed > 0,
                        "execution.receipt status does not match the failed-test count.");
                }
                break;
            case "unavailable":
                Require(!available && receipt is null && reason is not null,
                    "unavailable execution requires a reason and no receipt.");
                break;
        }

        return new(classification, available, receipt, reason);
    }

    private static Publication ParsePublication(JsonObject value)
    {
        RequireProperties(value, "publication", ["eligible", "resultDigest", "summaryRendererVersion"]);
        var digest = OptionalString(value, "resultDigest", "publication");
        if (!string.IsNullOrEmpty(digest))
        {
            Require(Sha256Regex.IsMatch(digest), "publication.resultDigest must be empty or a 64-character lowercase SHA-256.");
        }
        Require(Integer(value, "summaryRendererVersion", "publication") == RendererVersion,
            $"publication.summaryRendererVersion must be {RendererVersion}.");
        return new(Boolean(value, "eligible", "publication"), digest, RendererVersion);
    }

    private static void ValidateStatus(
        string status,
        string? reason,
        IReadOnlyList<Guide> guides,
        CandidateLedger candidates,
        Publication publication)
    {
        var allGuidesTerminal = guides.Count > 0 && guides.All(guide => guide.Status is "complete" or "excluded");
        switch (status)
        {
            case "FINDINGS":
                Require(reason is null, "FINDINGS must not include a reason.");
                Require(allGuidesTerminal, "FINDINGS requires every routed guide to be complete or explicitly excluded.");
                Require(candidates.Accepted.Count is > 0 and <= 5, "FINDINGS requires one to five accepted findings.");
                Require(publication.Eligible, "FINDINGS must be publication-eligible.");
                break;
            case "NO_FINDINGS":
                Require(reason is null, "NO_FINDINGS must not include a reason.");
                Require(allGuidesTerminal, "NO_FINDINGS requires every routed guide to be complete or explicitly excluded.");
                Require(candidates.Accepted.Count == 0, "NO_FINDINGS requires zero accepted findings.");
                Require(publication.Eligible, "NO_FINDINGS must be publication-eligible.");
                break;
            case "INCOMPLETE":
                Require(reason is not null, "INCOMPLETE requires a reason.");
                Require(guides.Any(guide => guide.Status == "incomplete"), "INCOMPLETE requires at least one incomplete guide.");
                Require(!publication.Eligible, "INCOMPLETE cannot be publication-eligible.");
                break;
            case "BLOCKED":
                Require(reason is not null, "BLOCKED requires a reason.");
                Require(!publication.Eligible, "BLOCKED cannot be publication-eligible.");
                break;
        }
    }

    private static JsonObject BuildCanonicalJson(ReviewResult result, string? resultDigest)
    {
        return new JsonObject
        {
            ["schemaVersion"] = SchemaVersion,
            ["status"] = result.Status,
            ["reason"] = result.Reason,
            ["target"] = new JsonObject
            {
                ["host"] = result.Target.Host,
                ["repository"] = result.Target.Repository,
                ["pullRequest"] = result.Target.PullRequest,
                ["head"] = result.Target.Head,
                ["mergeBase"] = result.Target.MergeBase,
                ["baseTip"] = result.Target.BaseTip,
            },
            ["guidance"] = new JsonObject
            {
                ["source"] = result.Guidance.Source,
                ["routingSha256"] = result.Guidance.RoutingSha256,
            },
            ["review"] = new JsonObject
            {
                ["path"] = result.ReviewPath,
            },
            ["guides"] = new JsonArray(result.Guides.Select(GuideJson).ToArray()),
            ["uncovered"] = new JsonArray(result.Uncovered.Select(item => new JsonObject
            {
                ["area"] = item.Area,
                ["reason"] = item.Reason,
            }).ToArray()),
            ["candidates"] = new JsonObject
            {
                ["accepted"] = new JsonArray(result.Candidates.Accepted.Select(AcceptedJson).ToArray()),
                ["duplicate"] = new JsonArray(result.Candidates.Duplicate.Select(DuplicateJson).ToArray()),
                ["unresolved"] = new JsonArray(result.Candidates.Unresolved.Select(item => new JsonObject
                {
                    ["id"] = item.Id,
                    ["summary"] = item.Summary,
                    ["missingEvidence"] = item.MissingEvidence,
                }).ToArray()),
                ["discarded"] = new JsonArray(result.Candidates.Discarded.Select(item => new JsonObject
                {
                    ["id"] = item.Id,
                    ["summary"] = item.Summary,
                    ["reason"] = item.Reason,
                }).ToArray()),
            },
            ["execution"] = ExecutionJson(result.Execution),
            ["limitations"] = new JsonArray(result.Limitations.Order(StringComparer.Ordinal).Select(item => JsonValue.Create(item)).ToArray()),
            ["publication"] = PublicationJson(result.Publication.Eligible, resultDigest),
        };
    }

    private static JsonObject PublicationJson(bool eligible, string? resultDigest)
    {
        var publication = new JsonObject
        {
            ["eligible"] = eligible,
        };
        if (resultDigest is not null)
        {
            publication["resultDigest"] = resultDigest;
        }
        publication["summaryRendererVersion"] = RendererVersion;
        return publication;
    }

    private static JsonObject GuideJson(Guide guide) => new()
    {
        ["path"] = guide.Path,
        ["status"] = guide.Status,
        ["evaluationStatus"] = guide.EvaluationStatus,
        ["requested"] = ConfigurationJson(guide.Requested),
        ["observed"] = ConfigurationJson(guide.Observed),
        ["completedChecks"] = new JsonArray(guide.CompletedChecks.Order(StringComparer.Ordinal).Select(item => JsonValue.Create(item)).ToArray()),
        ["exclusions"] = new JsonArray(guide.Exclusions.Select(exclusion => new JsonObject
        {
            ["scope"] = exclusion.Scope,
            ["reason"] = exclusion.Reason,
        }).ToArray()),
    };

    private static JsonObject ConfigurationJson(AgentConfiguration configuration) => new()
    {
        ["agentType"] = configuration.AgentType,
        ["model"] = configuration.Model,
        ["effort"] = configuration.Effort,
    };

    private static JsonObject AcceptedJson(AcceptedCandidate candidate) => new()
    {
        ["id"] = candidate.Id,
        ["severity"] = candidate.Severity,
        ["confidence"] = candidate.Confidence,
        ["summary"] = candidate.Summary,
        ["anchor"] = new JsonObject
        {
            ["path"] = candidate.Anchor.Path,
            ["line"] = candidate.Anchor.Line,
            ["endLine"] = candidate.Anchor.EndLine,
            ["side"] = "RIGHT",
        },
        ["trigger"] = candidate.Trigger,
        ["effect"] = candidate.Effect,
    };

    private static JsonObject DuplicateJson(DuplicateCandidate candidate) => new()
    {
        ["id"] = candidate.Id,
        ["summary"] = candidate.Summary,
        ["canonicalRootCause"] = candidate.CanonicalRootCause,
        ["existingFeedback"] = new JsonObject
        {
            ["kind"] = candidate.ExistingFeedback.Kind,
            ["id"] = candidate.ExistingFeedback.Id,
            ["status"] = candidate.ExistingFeedback.Status,
            ["reviewedHead"] = candidate.ExistingFeedback.ReviewedHead,
            ["url"] = candidate.ExistingFeedback.Url,
        },
    };

    private static JsonObject ExecutionJson(Execution execution) => new()
    {
        ["classification"] = execution.Classification,
        ["available"] = execution.Available,
        ["receipt"] = execution.Receipt is null ? null : new JsonObject
        {
            ["kind"] = execution.Receipt.Kind,
            ["status"] = execution.Receipt.Status,
            ["head"] = execution.Receipt.Head,
            ["receiptId"] = execution.Receipt.ReceiptId,
            ["url"] = execution.Receipt.Url,
            ["sha256"] = execution.Receipt.Sha256,
            ["testsPassed"] = execution.Receipt.TestsPassed,
            ["testsFailed"] = execution.Receipt.TestsFailed,
            ["testsSkipped"] = execution.Receipt.TestsSkipped,
        },
        ["reason"] = execution.Reason,
    };

    private static string Render(ReviewResult result, string digest)
    {
        var builder = new StringBuilder();
        builder.Append("### ").Append(Heading(result)).Append('\n').Append('\n');
        builder.Append("- **Snapshot:** [`").Append(result.Target.Head[..12]).Append("`](https://")
            .Append(result.Target.Host).Append('/').Append(result.Target.Repository).Append("/commit/")
            .Append(result.Target.Head).Append(") for PR #").Append(result.Target.PullRequest.ToString(CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("- **Coverage:** ").Append(CoverageSummary(result)).Append('\n');
        builder.Append("- **Disposition:** ").Append(result.Candidates.Accepted.Count).Append(" accepted");
        if (result.Candidates.Accepted.Count > 0)
        {
            builder.Append(" (")
                .Append(result.Candidates.Accepted.Count(candidate => candidate.Severity == "P1")).Append(" P1, ")
                .Append(result.Candidates.Accepted.Count(candidate => candidate.Severity == "P2")).Append(" P2, ")
                .Append(result.Candidates.Accepted.Count(candidate => candidate.Severity == "P3")).Append(" P3)");
        }
        builder.Append(", ")
            .Append(result.Candidates.Duplicate.Count).Append(" duplicate, ")
            .Append(result.Candidates.Unresolved.Count).Append(" unresolved, ")
            .Append(result.Candidates.Discarded.Count).Append(" discarded; ")
            .Append(result.Uncovered.Count).Append(" uncovered area(s)").Append('\n');
        builder.Append("- **Execution:** ").Append(ExecutionSummary(result.Execution)).Append('\n');
        builder.Append("- **Review method:** ").Append(result.ReviewPath).Append("; guidance ")
            .Append(EscapeMarkdown(result.Guidance.Source));
        if (result.Guides.Count == 0)
        {
            builder.Append("; no workers started");
        }
        else
        {
            builder.Append("; requested model(s) ")
                .Append(string.Join(", ", result.Guides.Select(guide => EscapeMarkdown(guide.Requested.Model)).Distinct(StringComparer.Ordinal)))
                .Append("; effort(s) ")
                .Append(string.Join(", ", result.Guides.Select(guide => EscapeMarkdown(guide.Requested.Effort)).Distinct(StringComparer.Ordinal)));
        }
        builder.Append('\n');
        builder.Append("- **Publication:** Local preview only; no GitHub writes performed.").Append('\n');

        AppendCandidateDetails(builder, result);
        AppendCoverageDetails(builder, result);
        AppendProvenance(builder, result, digest);
        return builder.ToString();
    }

    private static string Heading(ReviewResult result) => result.Status switch
    {
        "FINDINGS" => $"Review complete: {result.Candidates.Accepted.Count} new source-supported {Plural(result.Candidates.Accepted.Count, "finding", "findings")}",
        "NO_FINDINGS" when result.Candidates.Unresolved.Count > 0 =>
            $"Review complete: no new source-supported findings; {result.Candidates.Unresolved.Count} unresolved",
        "NO_FINDINGS" => "Review complete: no new source-supported findings",
        "INCOMPLETE" => $"Review incomplete: {EscapeMarkdown(result.Reason!)}",
        "BLOCKED" => $"Review blocked: {EscapeMarkdown(result.Reason!)}",
        _ => throw new InvalidOperationException($"Unknown status: {result.Status}"),
    };

    private static string CoverageSummary(ReviewResult result)
    {
        var complete = result.Guides.Count(guide => guide.Status == "complete");
        var fullyExcluded = result.Guides.Count(guide => guide.Status == "excluded");
        var excluded = result.Guides.Sum(guide => guide.Exclusions.Count);
        var summary = $"{complete}/{result.Guides.Count} guides completed";
        if (fullyExcluded > 0)
        {
            summary += $"; {fullyExcluded} fully excluded {Plural(fullyExcluded, "guide", "guides")}";
        }
        if (excluded > 0)
        {
            summary += $"; {excluded} excluded {Plural(excluded, "check", "checks")}";
        }
        if (result.Status is "INCOMPLETE" or "BLOCKED")
        {
            summary += "; not a completed review";
        }
        var incomplete = result.Guides.Where(guide => guide.Status == "incomplete").Select(guide => $"`{guide.Path}`").ToArray();
        if (incomplete.Length > 0)
        {
            summary += $"; incomplete: {string.Join(", ", incomplete.Take(3))}";
            if (incomplete.Length > 3)
            {
                summary += $" and {incomplete.Length - 3} more";
            }
        }
        return summary;
    }

    private static string ExecutionSummary(Execution execution) => execution.Classification switch
    {
        "source-only" => "No builds or tests run; source review only.",
        "unavailable" => $"Optional execution unavailable: {EscapeMarkdown(execution.Reason!)}",
        "local-detached-worktree" =>
            $"Validated local detached-worktree receipt {EscapeMarkdown(execution.Receipt!.ReceiptId)} ({execution.Receipt.Status}).",
        _ => throw new InvalidOperationException($"Unknown execution classification: {execution.Classification}"),
    };

    private static void AppendCandidateDetails(StringBuilder builder, ReviewResult result)
    {
        var count = result.Candidates.Accepted.Count
            + result.Candidates.Duplicate.Count
            + result.Candidates.Unresolved.Count
            + result.Candidates.Discarded.Count;
        if (count == 0)
        {
            return;
        }

        builder.Append('\n').Append("<details>").Append('\n')
            .Append("<summary>Candidate disposition</summary>").Append('\n').Append('\n');
        if (result.Status is "INCOMPLETE" or "BLOCKED")
        {
            builder.Append("_Retained candidate dispositions are not a completed or publishable review._").Append('\n').Append('\n');
        }
        AppendItems(builder, "Accepted", result.Candidates.Accepted, candidate =>
            $"`{candidate.Id}` {candidate.Severity} ({candidate.Confidence}/10) `{candidate.Anchor.Path}:{candidate.Anchor.Line}` - {EscapeMarkdown(candidate.Summary)}");
        AppendItems(builder, "Duplicate", result.Candidates.Duplicate, candidate =>
            $"`{candidate.Id}` - {EscapeMarkdown(candidate.Summary)}; root `{candidate.CanonicalRootCause}`, existing {candidate.ExistingFeedback.Kind} `{candidate.ExistingFeedback.Id}` ({candidate.ExistingFeedback.Status})");
        AppendItems(builder, "Unresolved", result.Candidates.Unresolved, candidate =>
            $"`{candidate.Id}` - {EscapeMarkdown(candidate.Summary)}; missing {EscapeMarkdown(candidate.MissingEvidence)}");
        AppendItems(builder, "Discarded", result.Candidates.Discarded, candidate =>
            $"`{candidate.Id}` - {EscapeMarkdown(candidate.Summary)}; {EscapeMarkdown(candidate.Reason)}");
        builder.Append("</details>").Append('\n');
    }

    private static void AppendCoverageDetails(StringBuilder builder, ReviewResult result)
    {
        var hasDetails = result.Guides.Count > 0
            || result.Uncovered.Count > 0
            || result.Limitations.Count > 0;
        if (!hasDetails)
        {
            return;
        }

        builder.Append('\n').Append("<details>").Append('\n')
            .Append("<summary>Coverage details</summary>").Append('\n').Append('\n');
        foreach (var guide in result.Guides)
        {
            builder.Append("- `").Append(guide.Path).Append("`: ").Append(guide.Status)
                .Append(", ").Append(guide.CompletedChecks.Count).Append(" completed ")
                .Append(Plural(guide.CompletedChecks.Count, "check", "checks"))
                .Append(", ").Append(guide.Exclusions.Count).Append(" ")
                .Append(Plural(guide.Exclusions.Count, "exclusion", "exclusions")).Append('\n');
            foreach (var exclusion in guide.Exclusions.Take(MaximumRenderedItems))
            {
                builder.Append("  - Excluded: ").Append(EscapeMarkdown(exclusion.Scope))
                    .Append(" - ").Append(EscapeMarkdown(exclusion.Reason)).Append('\n');
            }
            AppendOverflow(builder, guide.Exclusions.Count);
        }
        if (result.Guides.Count > 0 && (result.Uncovered.Count > 0 || result.Limitations.Count > 0))
        {
            builder.Append('\n');
        }
        AppendItems(builder, "Uncovered", result.Uncovered, item =>
            $"{EscapeMarkdown(item.Area)} - {EscapeMarkdown(item.Reason)}");
        AppendItems(builder, "Limitations", result.Limitations, EscapeMarkdown);
        builder.Append("</details>").Append('\n');
    }

    private static void AppendProvenance(StringBuilder builder, ReviewResult result, string digest)
    {
        builder.Append('\n').Append("<details>").Append('\n')
            .Append("<summary>Provenance</summary>").Append('\n').Append('\n')
            .Append("- Head: `").Append(result.Target.Head).Append("`").Append('\n')
            .Append("- Merge base: `").Append(result.Target.MergeBase).Append("`").Append('\n')
            .Append("- Base tip: `").Append(result.Target.BaseTip).Append("`").Append('\n')
            .Append("- Guidance routing SHA-256: `").Append(result.Guidance.RoutingSha256).Append("`").Append('\n')
            .Append("- Result digest: `").Append(digest).Append("`").Append('\n')
            .Append("- Renderer version: `").Append(RendererVersion).Append("`").Append('\n');
        foreach (var guide in result.Guides)
        {
            builder.Append("- `").Append(guide.Path).Append("`: requested ")
                .Append(EscapeMarkdown(guide.Requested.AgentType)).Append(" / ")
                .Append(EscapeMarkdown(guide.Requested.Model)).Append(" / ")
                .Append(EscapeMarkdown(guide.Requested.Effort)).Append("; observed ")
                .Append(EscapeMarkdown(guide.Observed.AgentType)).Append(" / ")
                .Append(EscapeMarkdown(guide.Observed.Model)).Append(" / ")
                .Append(EscapeMarkdown(guide.Observed.Effort)).Append("; ")
                .Append(guide.EvaluationStatus).Append('\n');
        }
        if (result.Execution.Receipt is not null)
        {
            builder.Append("- Execution receipt: `").Append(result.Execution.Receipt.ReceiptId)
                .Append("` (`").Append(result.Execution.Receipt.Sha256).Append("`)").Append('\n');
        }
        builder.Append("</details>").Append('\n');
    }

    private static void AppendItems<T>(StringBuilder builder, string heading, IReadOnlyList<T> items, Func<T, string> render)
    {
        if (items.Count == 0)
        {
            return;
        }

        builder.Append("**").Append(heading).Append("**").Append('\n');
        foreach (var item in items.Take(MaximumRenderedItems))
        {
            builder.Append("- ").Append(render(item)).Append('\n');
        }
        AppendOverflow(builder, items.Count);
        builder.Append('\n');
    }

    private static void AppendOverflow(StringBuilder builder, int count)
    {
        if (count > MaximumRenderedItems)
        {
            builder.Append("- ... and ").Append(count - MaximumRenderedItems).Append(" more in `")
                .Append(ResultFileName).Append("`").Append('\n');
        }
    }

    private static string EscapeMarkdown(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character is '\\' or '`' or '*' or '_' or '{' or '}' or '[' or ']' or '<' or '>' or '#' or '|' or '~')
            {
                builder.Append('\\');
            }
            builder.Append(character);
        }
        return builder.ToString();
    }

    private static string Plural(int count, string singular, string plural) => count == 1 ? singular : plural;

    private static int SeverityRank(string severity) => severity switch
    {
        "P1" => 1,
        "P2" => 2,
        "P3" => 3,
        _ => 4,
    };

    private static void WriteAtomically(string path, string content)
    {
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".tmp";
        File.WriteAllText(temporaryPath, content, Utf8NoBom);
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static void RejectDuplicateProperties(byte[] bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            RejectDuplicateProperties(document.RootElement, "$");
        }
        catch (JsonException error)
        {
            throw new InvalidOperationException($"Review result JSON is malformed: {error.Message}", error);
        }
    }

    private static void RejectDuplicateProperties(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                Require(names.Add(property.Name), $"Duplicate JSON property: {path}.{property.Name}");
                RejectDuplicateProperties(property.Value, $"{path}.{property.Name}");
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                RejectDuplicateProperties(item, $"{path}[{index++}]");
            }
        }
    }

    private static JsonObject AsObject(JsonNode? value, string description) =>
        value as JsonObject ?? throw new InvalidOperationException($"{description} must be an object.");

    private static JsonObject Object(JsonObject value, string property, string description) =>
        AsObject(value[property], $"{description}.{property}");

    private static JsonArray Array(JsonObject value, string property, string description) =>
        value[property] as JsonArray ?? throw new InvalidOperationException($"{description}.{property} must be an array.");

    private static string String(JsonObject value, string property, string description) =>
        value[property]?.GetValueKind() == JsonValueKind.String
            ? value[property]!.GetValue<string>()
            : throw new InvalidOperationException($"{description}.{property} must be a string.");

    private static string? OptionalString(JsonObject value, string property, string description)
    {
        if (value[property] is null)
        {
            return null;
        }
        return String(value, property, description);
    }

    private static string SafeString(JsonObject value, string property, string description, int maximumLength)
    {
        var text = String(value, property, description);
        ValidateSafeText(text, $"{description}.{property}", maximumLength);
        return text;
    }

    private static string? OptionalSafeString(JsonObject value, string property, string description, int maximumLength)
    {
        var text = OptionalString(value, property, description);
        if (text is not null)
        {
            ValidateSafeText(text, $"{description}.{property}", maximumLength);
        }
        return text;
    }

    private static void ValidateSafeText(string value, string description, int maximumLength)
    {
        Require(value.Length is > 0 && value.Length <= maximumLength,
            $"{description} must be 1..{maximumLength} characters.");
        Require(!value.Any(character => char.IsControl(character) || IsBidiControl(character)),
            $"{description} contains control characters.");
        Require(!MentionRegex.IsMatch(value), $"{description} contains an untrusted mention.");
        Require(!SlashCommandRegex.IsMatch(value), $"{description} contains an untrusted slash command.");
        Require(!WindowsPathRegex.IsMatch(value)
            && !PosixPathRegex.IsMatch(value)
            && !value.Contains(@"\\", StringComparison.Ordinal)
            && !value.Contains("file://", StringComparison.OrdinalIgnoreCase),
            $"{description} contains an absolute path.");
        Require(!value.Contains("://", StringComparison.Ordinal), $"{description} contains an unvalidated URL.");
        Require(!value.Contains("```", StringComparison.Ordinal)
            && !value.Contains("![", StringComparison.Ordinal)
            && !Regex.IsMatch(value, "<[^>]+>", RegexOptions.CultureInvariant),
            $"{description} contains untrusted Markdown or HTML.");
        Require(!SecretRegex.IsMatch(value), $"{description} contains a secret-like value.");
        Require(Utf8NoBom.GetByteCount(value) <= 2048, $"{description} exceeds 2048 UTF-8 bytes.");
    }

    private static bool IsBidiControl(char character) =>
        character is '\u061c' or '\u200e' or '\u200f'
            or >= '\u202a' and <= '\u202e'
            or >= '\u2066' and <= '\u2069';

    private static string SafeIdentifier(JsonObject value, string property, string description)
    {
        var identifier = String(value, property, description);
        Require(IdentifierRegex.IsMatch(identifier), $"{description}.{property} is not a valid identifier.");
        return identifier;
    }

    private static string SafeIdentifierOrUnknown(JsonObject value, string property, string description)
    {
        var identifier = String(value, property, description);
        Require(identifier == "unknown" || IdentifierRegex.IsMatch(identifier),
            $"{description}.{property} is not a valid identifier.");
        return identifier;
    }

    private static string RelativePath(JsonObject value, string property, string description)
    {
        var path = String(value, property, description);
        Require(path.Length is > 0 and <= 260
            && !Path.IsPathFullyQualified(path)
            && !path.StartsWith("/", StringComparison.Ordinal)
            && !path.Contains('\\')
            && !path.Contains('`')
            && !path.Split('/').Any(part => part is "" or "." or ".."),
            $"{description}.{property} must be a normalized repository-relative path.");
        ValidateSafeText(path, $"{description}.{property}", 260);
        return path;
    }

    private static int Integer(JsonObject value, string property, string description) =>
        value[property] is JsonValue jsonValue
            && jsonValue.GetValueKind() == JsonValueKind.Number
            && jsonValue.TryGetValue<int>(out var number)
                ? number
                : throw new InvalidOperationException($"{description}.{property} must be an integer.");

    private static int? OptionalInteger(JsonObject value, string property, string description)
    {
        if (value[property] is null)
        {
            return null;
        }
        return Integer(value, property, description);
    }

    private static int? OptionalNonNegativeInteger(JsonObject value, string property, string description)
    {
        var number = OptionalInteger(value, property, description);
        Require(number is null or >= 0, $"{description}.{property} must be non-negative.");
        return number;
    }

    private static bool Boolean(JsonObject value, string property, string description) =>
        value[property]?.GetValueKind() is JsonValueKind.True or JsonValueKind.False
            ? value[property]!.GetValue<bool>()
            : throw new InvalidOperationException($"{description}.{property} must be a Boolean.");

    private static string EnumString(JsonObject value, string property, string description, params string[] allowed)
    {
        var text = String(value, property, description);
        Require(allowed.Contains(text, StringComparer.Ordinal),
            $"{description}.{property} must be one of: {string.Join(", ", allowed)}.");
        return text;
    }

    private static string Sha40(JsonObject value, string property, string description)
    {
        var sha = String(value, property, description);
        Require(Sha40Regex.IsMatch(sha), $"{description}.{property} must be a 40-character lowercase SHA.");
        return sha;
    }

    private static string Sha256(JsonObject value, string property, string description)
    {
        var sha = String(value, property, description);
        Require(Sha256Regex.IsMatch(sha), $"{description}.{property} must be a 64-character lowercase SHA-256.");
        return sha;
    }

    private static IReadOnlyList<string> SafeStringArray(
        JsonArray values,
        string description,
        int maximumItems,
        int maximumLength)
    {
        Require(values.Count <= maximumItems, $"{description} must contain at most {maximumItems} entries.");
        return values.Select((node, index) =>
        {
            Require(node?.GetValueKind() == JsonValueKind.String, $"{description}[{index}] must be a string.");
            var text = node!.GetValue<string>();
            ValidateSafeText(text, $"{description}[{index}]", maximumLength);
            return text;
        })
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();
    }

    private static string? OptionalUrl(JsonObject value, string property, string description, Target target)
    {
        var text = OptionalString(value, property, description);
        if (text is null)
        {
            return null;
        }

        Require(Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.Equals(target.Host, StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.StartsWith($"/{target.Repository}/", StringComparison.Ordinal),
            $"{description}.{property} must be an HTTPS URL under the target repository.");
        Require(text.Length <= 500, $"{description}.{property} exceeds 500 characters.");
        return uri!.AbsoluteUri;
    }

    private static bool KnownMismatch(string requested, string observed) =>
        observed != "unknown" && !string.Equals(requested, observed, StringComparison.Ordinal);

    private static bool KnownEffortMismatch(string requested, string observed) =>
        observed != "unknown"
            && requested != "not supplied (runtime default)"
            && !string.Equals(requested, observed, StringComparison.Ordinal);

    private static void RequireProperties(JsonObject value, string description, IReadOnlyCollection<string> properties)
    {
        var actual = value.Select(property => property.Key).ToHashSet(StringComparer.Ordinal);
        Require(actual.SetEquals(properties),
            $"{description} properties must be exactly: {string.Join(", ", properties.Order(StringComparer.Ordinal))}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
