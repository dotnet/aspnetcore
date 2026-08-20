// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using ComponentsAIClaimApp.Data;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DojoClient.E2E.Tests.ServiceOverrides;

internal class ClaimAppModelOverrides
{
    public static void UseTestModel(IServiceCollection services)
    {
        services.RemoveAll<IClaimAssistantBackend>();
        services.AddSingleton<IClaimAssistantBackend, TestClaimAssistantBackend>();
        services.AddOptions<ClaimAgentOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                options.BaseAddress = configuration["E2E_TEST_APP_URL"]
                    ?? throw new InvalidOperationException(
                        "The ClaimApp test server did not provide its direct application URL.");
            });
    }

    private sealed class TestClaimAssistantBackend : IClaimAssistantBackend
    {
        private readonly ConcurrentDictionary<string, byte> _transientErrors = new();

        public string ModelName => "Local simulator";

        public Task<bool> ShouldAnalyzeEvidenceAsync(
            IReadOnlyList<ChatMessage> messages,
            ClaimState state,
            int currentMediaCount,
            CancellationToken cancellationToken)
        {
            var description = messages.LastOrDefault()?.Text ?? string.Empty;
            return Task.FromResult(
                currentMediaCount > 0 ||
                ClaimTerms.Any(term =>
                    description.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<string> GenerateResponseAsync(
            IReadOnlyList<ChatMessage> messages,
            ClaimState state,
            ClaimResponsePurpose purpose,
            CancellationToken cancellationToken)
        {
            var response = purpose switch
            {
                ClaimResponsePurpose.Conversation when IsGreeting(
                    messages.LastOrDefault()?.Text ?? string.Empty) =>
                    "Hi. I can inspect vehicle photos, transcribe a voice description, estimate a repair range, and research likely replacement parts. Add one or more photos, record a voice note, or describe the damaged area and what happened. Include the vehicle year, make, model, and repair location for parts and pricing.",
                ClaimResponsePurpose.Conversation =>
                    "I need a little more claim detail before I can assess the vehicle. Add one or more photos, record a voice note, or describe the damaged area and what happened. Include the vehicle year, make, model, and repair location for parts and pricing.",
                ClaimResponsePurpose.MoreEvidence =>
                    $"{state.AssessmentSummary} {state.NextPhotoSuggestion ?? "Add a clear wide photo and a close-up of the damaged area before reviewing an assessment."}",
                ClaimResponsePurpose.AssessmentReady =>
                    $"{state.AssessmentSummary} The current confidence is {state.Confidence}%. {DescribeEstimate(state)}",
                ClaimResponsePurpose.Decision when state.Status == "Add more evidence" =>
                    "Add another vehicle photo or update the description. I will reassess the complete evidence set.",
                ClaimResponsePurpose.Decision when state.Decision == "Approved" =>
                    "The assessment is approved and ready for the next claim step.",
                ClaimResponsePurpose.Decision =>
                    $"The assessment was rejected. I recorded the reason: {state.RejectionReason ?? "No reason supplied."}",
                _ => throw new ArgumentOutOfRangeException(nameof(purpose)),
            };
            return Task.FromResult(response);
        }

        public async Task<ClaimDamageAnalysis> AnalyzeAsync(
            string description,
            IReadOnlyList<DataContent> images,
            IReadOnlyList<DataContent> audio,
            CancellationToken cancellationToken)
        {
            await Task.Delay(500, cancellationToken);
            if (description.Contains("error", StringComparison.OrdinalIgnoreCase) &&
                _transientErrors.TryAdd(description, 0))
            {
                throw new InvalidOperationException(
                    "The test damage model failed while reading the accident description.");
            }

            var hasDescription = !string.IsNullOrWhiteSpace(description) &&
                !string.Equals(
                    description,
                    "Review the attached claim evidence.",
                    StringComparison.OrdinalIgnoreCase);
            var areas = hasDescription ? IdentifyVehicleAreas(description) : [];
            var findings = areas.Select(area => new ClaimDamageFinding
            {
                Area = area,
                DamageType = "Reported damage",
                Severity = areas.Count > 3 ? "Significant" : "Moderate",
                Confidence = images.Count > 0 ? 62 : 48,
                Evidence = "Reported in the claim description.",
            }).ToList();

            return new ClaimDamageAnalysis
            {
                Summary = images.Count > 0 && !hasDescription
                    ? $"Received {images.Count} vehicle photo{(images.Count == 1 ? string.Empty : "s")}."
                    : images.Count > 0
                        ? $"Received {images.Count} vehicle photo{(images.Count == 1 ? string.Empty : "s")} and mapped the damage reported in the description."
                        : $"Likely {string.Join(", ", areas.Select(ToDisplayName))} damage based on the description.",
                Confidence = hasDescription ? (images.Count > 0 ? 62 : 78) : 0,
                AffectedAreas = areas,
                Findings = findings,
                NextPhotoSuggestion = images.Count == 0
                    ? "Add wide and close-up photos of the damaged area."
                    : "Add a wider photo showing the damaged panel in context.",
                NeedsHumanReview = true,
            };
        }

        public Task<string> TranscribeAsync(
            DataContent recording,
            CancellationToken cancellationToken)
            => recording.Data.Length <= 4
                ? throw new InvalidOperationException(
                    "Configure Microsoft Foundry before transcribing voice evidence.")
                : Task.FromResult("The front bumper is cracked.");

        private static string DescribeEstimate(ClaimState state)
            => state.RepairEstimate is { High: > 0 } estimate
                ? $"The researched repair range is {estimate.Currency} {estimate.Low:N0}–{estimate.High:N0}. Review the sources, then approve, reject, or add evidence."
                : "Review the findings, then approve, reject, or add evidence.";

        private static bool IsGreeting(string description)
        {
            var normalized = description.Trim().TrimEnd('.', '!', '?');
            return normalized.Equals("hi", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("hey", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("hello", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("good morning", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("good afternoon", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("good evening", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> IdentifyVehicleAreas(string description)
        {
            var areas = new List<string>();
            AddAreaWhen(description, areas, "front-bumper", "front", "bumper", "backed into");
            AddAreaWhen(description, areas, "hood", "hood", "bonnet");
            AddAreaWhen(description, areas, "windshield", "windshield", "windscreen", "glass");
            AddAreaWhen(description, areas, "left-fender", "driver", "left", "fender");
            AddAreaWhen(description, areas, "left-door", "driver door", "left door", "side");
            AddAreaWhen(description, areas, "right-fender", "passenger", "right fender");
            AddAreaWhen(description, areas, "right-door", "passenger door", "right door");
            AddAreaWhen(description, areas, "rear-bumper", "rear", "behind", "back bumper");
            return areas.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void AddAreaWhen(
            string description,
            List<string> areas,
            string area,
            params string[] terms)
        {
            if (terms.Any(term =>
                description.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                areas.Add(area);
            }
        }

        private static string ToDisplayName(string area)
            => area.Replace('-', ' ');

        private static readonly string[] ClaimTerms =
        [
            "accident",
            "collision",
            "crash",
            "damage",
            "damaged",
            "hit",
            "impact",
            "dent",
            "scratch",
            "crack",
            "broken",
            "bumper",
            "hood",
            "fender",
            "door",
            "windshield",
            "headlight",
            "taillight",
        ];
    }
}
