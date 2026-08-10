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
            else if (operation.TryGetProperty("value", out var value))
            {
                root[propertyName] = JsonNode.Parse(value.GetRawText());
            }
        }

        return root.Deserialize<ClaimState>(Options) ?? Clone(current);
    }
}
