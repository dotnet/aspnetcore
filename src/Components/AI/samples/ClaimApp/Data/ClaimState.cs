// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace ComponentsAIClaimApp.Data;

/// <summary>
/// Contains the claim state synchronized between the AG-UI agent and the sample UI.
/// </summary>
public sealed class ClaimState
{
    /// <summary>
    /// Gets or sets the claim number.
    /// </summary>
    public string ClaimNumber { get; set; } = "POC-1042";

    /// <summary>
    /// Gets or sets the current processing status.
    /// </summary>
    public string Status { get; set; } = "Ready";

    /// <summary>
    /// Gets or sets the accident description.
    /// </summary>
    public string AccidentSummary { get; set; } = "Describe the accident to start the assessment.";

    /// <summary>
    /// Gets or sets the generated assessment summary.
    /// </summary>
    public string AssessmentSummary { get; set; } = "No assessment yet.";

    /// <summary>
    /// Gets or sets the summary of image and voice evidence submitted with the claim.
    /// </summary>
    public string EvidenceSummary { get; set; } = "No evidence attached.";

    /// <summary>
    /// Gets or sets the vehicle areas likely affected by the accident.
    /// </summary>
    public List<string> AffectedAreas { get; set; } = [];

    /// <summary>
    /// Gets or sets the assessment confidence percentage.
    /// </summary>
    public int Confidence { get; set; }

    /// <summary>
    /// Gets or sets the user's assessment decision.
    /// </summary>
    public string Decision { get; set; } = "Pending";

    /// <summary>
    /// Gets or sets the reason supplied when rejecting the assessment.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Gets or sets the visible damage findings correlated across the submitted photos.
    /// </summary>
    public List<ClaimDamageFinding> DamageFindings { get; set; } = [];

    /// <summary>
    /// Gets or sets the most useful next photo requested by the damage analyzer.
    /// </summary>
    public string? NextPhotoSuggestion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a human adjuster should review the findings.
    /// </summary>
    public bool NeedsHumanReview { get; set; }

    /// <summary>
    /// Gets or sets the transcript produced from submitted voice evidence.
    /// </summary>
    public string? VoiceTranscript { get; set; }

    /// <summary>
    /// Gets or sets the grounded repair-cost estimate.
    /// </summary>
    public ClaimRepairEstimate? RepairEstimate { get; set; }

    /// <summary>
    /// Gets or sets likely replacement parts found through grounded market research.
    /// </summary>
    public List<ClaimReplacementPart> ReplacementParts { get; set; } = [];

    /// <summary>
    /// Gets or sets the public sources used for parts and repair-cost research.
    /// </summary>
    public List<ClaimResearchSource> ResearchSources { get; set; } = [];

    /// <summary>
    /// Gets or sets a research limitation that should be shown to the user.
    /// </summary>
    public string? ResearchWarning { get; set; }
}

/// <summary>
/// Describes one visible or reported vehicle damage finding.
/// </summary>
public sealed class ClaimDamageFinding
{
    /// <summary>
    /// Gets or sets the vehicle area identifier.
    /// </summary>
    public string Area { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the observed damage type.
    /// </summary>
    public string DamageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated severity.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence percentage.
    /// </summary>
    public int Confidence { get; set; }

    /// <summary>
    /// Gets or sets the evidence supporting the finding.
    /// </summary>
    public string Evidence { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the photo names supporting the finding.
    /// </summary>
    public List<string> PhotoNames { get; set; } = [];
}

/// <summary>
/// Describes a market-grounded repair estimate rather than a final settlement value.
/// </summary>
public sealed class ClaimRepairEstimate
{
    /// <summary>
    /// Gets or sets the low end of the estimated repair range.
    /// </summary>
    public decimal Low { get; set; }

    /// <summary>
    /// Gets or sets the high end of the estimated repair range.
    /// </summary>
    public decimal High { get; set; }

    /// <summary>
    /// Gets or sets the ISO-style currency code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets a concise explanation of the estimate basis.
    /// </summary>
    public string Basis { get; set; } = string.Empty;
}

/// <summary>
/// Describes one likely replacement part and its current market range.
/// </summary>
public sealed class ClaimReplacementPart
{
    /// <summary>
    /// Gets or sets the part name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the low observed part price.
    /// </summary>
    public decimal PriceLow { get; set; }

    /// <summary>
    /// Gets or sets the high observed part price.
    /// </summary>
    public decimal PriceHigh { get; set; }

    /// <summary>
    /// Gets or sets the price currency.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets fitment or verification guidance.
    /// </summary>
    public string Fitment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source title.
    /// </summary>
    public string SourceTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source URL.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;
}

/// <summary>
/// Identifies a public source used for grounded claim research.
/// </summary>
public sealed class ClaimResearchSource
{
    /// <summary>
    /// Gets or sets the source title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;
}

public sealed class ClaimDamageAnalysis
{
    public string Summary { get; set; } = string.Empty;

    public int Confidence { get; set; }

    public List<string> AffectedAreas { get; set; } = [];

    public List<ClaimDamageFinding> Findings { get; set; } = [];

    public string? NextPhotoSuggestion { get; set; }

    public bool NeedsHumanReview { get; set; }

    public string? VoiceTranscript { get; set; }

    public ClaimRepairEstimate? RepairEstimate { get; set; }

    public List<ClaimReplacementPart> ReplacementParts { get; set; } = [];

    public List<ClaimResearchSource> ResearchSources { get; set; } = [];

    public string? ResearchWarning { get; set; }
}

internal sealed class ClaimMarketResearch
{
    public ClaimRepairEstimate? RepairEstimate { get; set; }

    public List<ClaimReplacementPart> ReplacementParts { get; set; } = [];

    public string? Warning { get; set; }
}

/// <summary>
/// Represents the result of identifying affected vehicle areas.
/// </summary>
public sealed class VehicleAreaAssessment
{
    /// <summary>
    /// Gets or sets the affected vehicle area identifiers.
    /// </summary>
    public List<string> Areas { get; set; } = [];

    /// <summary>
    /// Gets or sets the estimated damage severity.
    /// </summary>
    public string Severity { get; set; } = "Moderate";
}

public enum ClaimResponsePurpose
{
    Conversation,
    MoreEvidence,
    AssessmentReady,
    Decision,
}

internal static class ClaimStateJson
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static ClaimState Clone(ClaimState state)
        => JsonSerializer.Deserialize<ClaimState>(JsonSerializer.Serialize(state, Options), Options)
            ?? new ClaimState();

    public static ClaimState ApplyDelta(ClaimState current, JsonElement delta)
    {
        var root = JsonSerializer.SerializeToNode(current, Options)?.AsObject()
            ?? new JsonObject();

        foreach (var operation in delta.EnumerateArray())
        {
            if (!operation.TryGetProperty("op", out var opElement) ||
                !operation.TryGetProperty("path", out var pathElement))
            {
                continue;
            }

            var path = pathElement.GetString();
            if (string.IsNullOrEmpty(path) || path[0] != '/' || path.IndexOf('/', 1) >= 0)
            {
                continue;
            }

            var propertyName = path[1..].Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
            var operationName = opElement.GetString();

            if (operationName == "remove")
            {
                root.Remove(propertyName);
            }
            else if (operationName is "add" or "replace" &&
                operation.TryGetProperty("value", out var value))
            {
                root[propertyName] = JsonNode.Parse(value.GetRawText());
            }
        }

        return root.Deserialize<ClaimState>(Options) ?? Clone(current);
    }
}
