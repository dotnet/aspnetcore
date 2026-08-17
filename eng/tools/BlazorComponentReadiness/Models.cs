// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorComponentReadiness;

internal sealed class SkillLayout
{
    private SkillLayout(string root)
    {
        Root = root;
        ChecklistPath = Path.Combine(root, "references", "checklist.md");
        AreasIndexPath = Path.Combine(root, "references", "areas", "index.md");
        SkillPath = Path.Combine(root, "SKILL.md");
        ReportTemplatePath = Path.Combine(root, "references", "report-template.md");
        VallyPath = Path.Combine(root, "evals", "regression.vally.yaml");
        OverlayPaths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["scaffolder"] = Path.Combine(root, "references", "overlays", "scaffolder.md"),
            ["ai-skill"] = Path.Combine(root, "references", "overlays", "ai-skill.md"),
        };
        OverlayPrefixes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["scaffolder"] = "SCF",
            ["ai-skill"] = "AI",
        };
    }

    internal string Root { get; }

    internal string ChecklistPath { get; }

    internal string AreasIndexPath { get; }

    internal string SkillPath { get; }

    internal string ReportTemplatePath { get; }

    internal string VallyPath { get; }

    internal IReadOnlyDictionary<string, string> OverlayPaths { get; }

    internal IReadOnlyDictionary<string, string> OverlayPrefixes { get; }

    internal static SkillLayout Create(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        return new SkillLayout(Path.GetFullPath(root));
    }
}

internal sealed record Requirement(
    string Identifier,
    string Text,
    string? Scope,
    bool IsCore);

internal sealed record RubricSnapshot(
    string Path,
    string Version,
    int ScopeSchemaVersion,
    string Sha256,
    IReadOnlyList<Requirement> Requirements,
    ReadOnlyMemory<byte> Bytes);

internal sealed record ScorecardRow(
    string Identifier,
    string Requirement,
    string Scope,
    string Status,
    string Evidence,
    string MaintainerAction,
    string ReviewerFollowUp,
    int LineNumber);

internal sealed record EvidenceLedger(
    IReadOnlyDictionary<string, int> Identifiers,
    IReadOnlyList<string> Errors);

internal sealed record ReportSnapshot(
    string Path,
    string Content,
    ReadOnlyMemory<byte> Bytes);

internal sealed record VallyStimulus(
    string Name,
    IReadOnlyDictionary<string, string> Tags,
    int RubricCount,
    IReadOnlyList<string> FixtureSources);
