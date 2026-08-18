// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorComponentReadiness;

internal static class SkillValidator
{
    internal const string VallyPackage = "@microsoft/vally-cli@0.13.0";
    internal const int ExpectedCoreRequirementCount =
        ScorecardValidator.ExpectedCoreRequirementCount;
    private static readonly HashSet<string> ExpectedCorePrefixes = new(
        ["LP", "PI", "SEC", "A11Y", "BEQ", "TA", "PERF", "CI", "SUP"],
        StringComparer.Ordinal);
    private static readonly IReadOnlyDictionary<string, (string Prefix, int Count)>
        ExpectedOverlays =
            new Dictionary<string, (string Prefix, int Count)>(StringComparer.Ordinal)
            {
                ["scaffolder"] = ("SCF", 6),
                ["ai-skill"] = ("AI", 6),
            };
    private static readonly HashSet<string> RequiredTags = new(
        [
            "eval_id",
            "area",
            "score_family",
            "tier",
            "requirement_prefixes",
            "provenance_kind",
            "provenance_source",
            "positive_controls",
            "negative_controls",
        ],
        StringComparer.Ordinal);
    private static readonly Regex StimulusPattern = new(
        "^  - name: \"([^\"]+)\"[ \\t]*(?:#.*)?$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex StimulusDeclarationPattern = new(
        "^  - .*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex StimuliMarkerPattern = new(
        "^stimuli:[ \\t]*(?:#.*)?$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex TagPattern = new(
        "^      ([a-z_]+): \"([^\"]*)\"\\s*$",
        RegexOptions.CultureInvariant);
    private static readonly Regex StimulusPropertyPattern = new(
        "^    ([a-z_]+):[ \\t]*(.*?)[ \\t]*$",
        RegexOptions.CultureInvariant);
    private static readonly Regex RubricMarkerPattern = new(
        "^    rubric:\\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex RubricItemPattern = new(
        "^      - \"(.+)\"\\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex FixturePattern = new(
        "^        - src: \"([^\"]+)\"\\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex AreaReferencePattern = new(
        "`([^`]+\\.md)`",
        RegexOptions.CultureInvariant);

    internal static IReadOnlyList<VallyStimulus> ParseVallyStimuli(string path)
    {
        var content = NormalizeLineEndings(File.ReadAllText(path, Encoding.UTF8));
        var stimuliMarkers = StimuliMarkerPattern.Matches(content);
        if (stimuliMarkers.Count != 1)
        {
            throw new InvalidDataException(
                $"Expected exactly one top-level stimuli mapping; found " +
                stimuliMarkers.Count);
        }

        var stimuliStart = stimuliMarkers[0].Index + stimuliMarkers[0].Length;
        var declarations = StimulusDeclarationPattern
            .Matches(content)
            .Where(declaration => declaration.Index > stimuliStart)
            .ToArray();
        var matches = StimulusPattern.Matches(content);
        var governedMatches = matches
            .Where(match => match.Index > stimuliStart)
            .ToArray();
        foreach (var declaration in declarations)
        {
            if (!governedMatches.Any(match => match.Index == declaration.Index))
            {
                throw new InvalidDataException(
                    $"{path}: unsupported stimulus declaration: " +
                    declaration.Value.Trim());
            }
        }

        if (declarations.Length != governedMatches.Length)
        {
            throw new InvalidDataException(
                $"{path}: stimulus declarations could not be governed completely");
        }

        if (governedMatches.Length > 0 &&
            !ContainsOnlyTrivia(content[stimuliStart..governedMatches[0].Index]))
        {
            throw new InvalidDataException(
                $"{path}: unparsed content appears before the first stimulus");
        }

        var stimuli = new List<VallyStimulus>(governedMatches.Length);
        for (var index = 0; index < governedMatches.Length; index++)
        {
            var match = governedMatches[index];
            var start = match.Index + match.Length;
            var end = index + 1 < governedMatches.Length
                ? governedMatches[index + 1].Index
                : content.Length;
            var block = content[start..end];
            if (!ContainsOnlyStimulusContent(block))
            {
                throw new InvalidDataException(
                    $"{path}: unparsed content appears after stimulus " +
                    match.Groups[1].Value);
            }

            var tags = ParseTags(match.Groups[1].Value, block);
            var rubricMarker = RubricMarkerPattern.Match(block);
            var rubricItems = rubricMarker.Success
                ? RubricItemPattern
                    .Matches(
                        block[(rubricMarker.Index + rubricMarker.Length)..])
                    .Select(item => item.Groups[1].Value)
                    .ToArray()
                : [];
            var fixtures = FixturePattern
                .Matches(block)
                .Select(fixture => fixture.Groups[1].Value)
                .ToArray();
            stimuli.Add(new VallyStimulus(
                match.Groups[1].Value,
                tags,
                rubricItems.Length,
                fixtures,
                ParsePrompt(match.Groups[1].Value, block),
                rubricItems));
        }

        return stimuli;
    }

    private static string ParsePrompt(string stimulusName, string block)
    {
        var lines = new List<string>();
        using (var reader = new StringReader(block))
        {
            while (reader.ReadLine() is { } line)
            {
                lines.Add(line);
            }
        }

        var marker = $"    prompt: |-";
        var promptIndex = lines.FindIndex(line =>
            string.Equals(line, marker, StringComparison.Ordinal));
        if (promptIndex < 0)
        {
            throw new InvalidDataException(
                $"{stimulusName}: supported prompt block was not found");
        }

        var prompt = new List<string>();
        for (var index = promptIndex + 1; index < lines.Count; index++)
        {
            var line = lines[index];
            if (!string.IsNullOrWhiteSpace(line) && CountIndent(line) <= 4)
            {
                break;
            }

            prompt.Add(line.Length >= 6 ? line[6..] : string.Empty);
        }

        return string.Join('\n', prompt);
    }

    private static string NormalizeLineEndings(string content)
    {
        return content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private static bool ContainsOnlyTrivia(string content)
    {
        using var reader = new StringReader(content);
        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line) &&
                !line.TrimStart().StartsWith('#'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsOnlyStimulusContent(string content)
    {
        using var reader = new StringReader(content);
        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line) &&
                !line.TrimStart().StartsWith('#') &&
                CountIndent(line) < 4)
            {
                return false;
            }
        }

        return true;
    }

    internal static IReadOnlyList<string> Validate(SkillLayout layout)
    {
        var errors = new List<string>();
        try
        {
            var prefixes = ValidateRequirementSequences(layout, errors);
            ValidateAreaMapping(layout, prefixes, errors);
            ValidateVally(layout, prefixes, errors);
            ValidateWiring(layout, errors);
        }
        catch (Exception exception) when (
            exception is IOException or
            InvalidDataException or
            FormatException or
            UnauthorizedAccessException)
        {
            errors.Add(exception.Message);
        }

        return errors;
    }

    private static HashSet<string> ValidateRequirementSequences(
        SkillLayout layout,
        ICollection<string> errors)
    {
        var coreRequirements = ScorecardValidator.LoadCoreRubric(
            layout.ChecklistPath).Requirements;
        var corePrefixes = coreRequirements
            .Select(requirement => Prefix(requirement.Identifier))
            .ToHashSet(StringComparer.Ordinal);
        if (!corePrefixes.SetEquals(ExpectedCorePrefixes))
        {
            errors.Add(
                "Core requirement prefixes differ: expected " +
                $"{Join(ExpectedCorePrefixes)}, found {Join(corePrefixes)}");
        }

        if (coreRequirements.Count != ExpectedCoreRequirementCount)
        {
            errors.Add(
                $"Expected {ExpectedCoreRequirementCount} core requirements; " +
                $"found {coreRequirements.Count}");
        }

        foreach (var requirement in coreRequirements)
        {
            var expectedScope =
                CanonicalRequirementSchema.RequirementScopes[requirement.Identifier];
            if (!string.Equals(requirement.Scope, expectedScope, StringComparison.Ordinal))
            {
                errors.Add(
                    $"{requirement.Identifier} has canonical scope '{requirement.Scope}'; " +
                    $"expected '{expectedScope}'");
            }
        }

        var repositoryWideCount = coreRequirements.Count(requirement =>
            string.Equals(requirement.Scope, "repository-wide", StringComparison.Ordinal));
        var expectedRepositoryWideCount =
            CanonicalRequirementSchema.RequirementScopes.Values.Count(scope =>
                string.Equals(scope, "repository-wide", StringComparison.Ordinal));
        if (repositoryWideCount != expectedRepositoryWideCount)
        {
            errors.Add(
                $"Expected {expectedRepositoryWideCount} repository-wide core requirements; " +
                $"found {repositoryWideCount}");
        }

        foreach (var (overlay, expected) in ExpectedOverlays)
        {
            var requirements = ScorecardValidator.LoadOverlayRequirements(
                layout.OverlayPaths[overlay],
                layout.OverlayPrefixes[overlay]);
            var prefixes = requirements
                .Select(requirement => Prefix(requirement.Identifier))
                .ToHashSet(StringComparer.Ordinal);
            if (!prefixes.SetEquals([expected.Prefix]))
            {
                errors.Add(
                    $"{overlay} overlay prefixes differ: expected {expected.Prefix}, " +
                    $"found {Join(prefixes)}");
            }

            if (requirements.Count != expected.Count)
            {
                errors.Add(
                    $"Expected {expected.Count} requirements in {overlay} overlay; " +
                    $"found {requirements.Count}");
            }
        }

        var allRequirements = ScorecardValidator.LoadRequirementSet(
            layout,
            layout.OverlayPaths.Keys);
        var numbers = allRequirements
            .GroupBy(requirement => Prefix(requirement.Identifier), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(requirement => Number(requirement.Identifier)).ToArray(),
                StringComparer.Ordinal);
        foreach (var (prefix, values) in numbers)
        {
            if (!values.SequenceEqual(values.Order()))
            {
                errors.Add(
                    $"{prefix} requirement IDs are not in ascending order: " +
                    string.Join(", ", values));
            }
        }

        return numbers.Keys.ToHashSet(StringComparer.Ordinal);
    }

    private static void ValidateAreaMapping(
        SkillLayout layout,
        IReadOnlySet<string> prefixes,
        ICollection<string> errors)
    {
        var content = File.ReadAllText(layout.AreasIndexPath, Encoding.UTF8);
        foreach (var prefix in prefixes)
        {
            if (!content.Contains($"`{prefix}-*`", StringComparison.Ordinal))
            {
                errors.Add($"Area index does not map {prefix}-*");
            }
        }

        var areaDirectory = Path.GetDirectoryName(layout.AreasIndexPath)!;
        var resolvedSkillRoot = FileSystemUtilities.ResolveExistingPath(layout.Root);
        var resolvedAreaDirectory = FileSystemUtilities.ResolveExistingPath(areaDirectory);
        if (!FileSystemUtilities.IsWithinDirectory(
            resolvedSkillRoot,
            resolvedAreaDirectory))
        {
            errors.Add(
                $"Area directory must remain under skill root {layout.Root}: " +
                areaDirectory);
            return;
        }

        foreach (Match match in AreaReferencePattern.Matches(content))
        {
            var reference = match.Groups[1].Value;
            var displayReference = reference.Replace("\0", "\\0", StringComparison.Ordinal);
            if (reference.Contains('\0'))
            {
                errors.Add(
                    $"Invalid area playbook reference '{displayReference}': contains NUL");
                continue;
            }

            string referenced;
            try
            {
                if (Path.IsPathRooted(reference))
                {
                    errors.Add(
                        $"Area playbook must remain under {areaDirectory}: " +
                        displayReference);
                    continue;
                }

                referenced = Path.GetFullPath(Path.Combine(areaDirectory, reference));
            }
            catch (ArgumentException exception)
            {
                errors.Add(
                    $"Invalid area playbook reference '{displayReference}': " +
                    exception.Message);
                continue;
            }

            if (!FileSystemUtilities.IsWithinDirectory(areaDirectory, referenced))
            {
                errors.Add(
                    $"Area playbook must remain under {areaDirectory}: " +
                    displayReference);
                continue;
            }

            if (!File.Exists(referenced))
            {
                errors.Add($"Missing area playbook: {referenced}");
                continue;
            }

            var resolvedReference = FileSystemUtilities.ResolveExistingPath(referenced);
            if (!FileSystemUtilities.IsWithinDirectory(
                resolvedAreaDirectory,
                resolvedReference))
            {
                errors.Add(
                    $"Area playbook must remain under {areaDirectory}: " +
                    displayReference);
            }
        }
    }

    private static void ValidateVally(
        SkillLayout layout,
        IReadOnlySet<string> prefixes,
        ICollection<string> errors)
    {
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        foreach (var marker in new[]
        {
            $"# Validated with {VallyPackage}.",
            "  runs: 5",
            "  model: gpt-5.6-sol",
            "  judge_model: claude-opus-5",
        })
        {
            if (!content.Contains(marker, StringComparison.Ordinal))
            {
                errors.Add($"Vally suite is missing pinned marker: {marker}");
            }
        }

        var stimuli = ParseVallyStimuli(layout.VallyPath);
        if (stimuli.Count == 0)
        {
            errors.Add("Vally suite contains no stimuli");
            return;
        }

        var evalIds = new List<string>();
        var coveredPrefixes = new HashSet<string>(StringComparer.Ordinal);
        var scoreFamilies = new HashSet<string>(StringComparer.Ordinal);
        var tiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var stimulus in stimuli)
        {
            var missingTags = RequiredTags
                .Where(tag => !stimulus.Tags.ContainsKey(tag))
                .Order(StringComparer.Ordinal)
                .ToArray();
            if (missingTags.Length > 0)
            {
                errors.Add(
                    $"{stimulus.Name}: missing Vally tags {string.Join(", ", missingTags)}");
                continue;
            }

            evalIds.Add(stimulus.Tags["eval_id"]);
            var requirementPrefixes = stimulus.Tags["requirement_prefixes"]
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet(StringComparer.Ordinal);
            var unknownPrefixes = requirementPrefixes
                .Where(prefix => !prefixes.Contains(prefix))
                .Order(StringComparer.Ordinal)
                .ToArray();
            if (unknownPrefixes.Length > 0)
            {
                errors.Add(
                    $"{stimulus.Name}: unknown requirement prefixes " +
                    string.Join(", ", unknownPrefixes));
            }

            coveredPrefixes.UnionWith(requirementPrefixes);
            scoreFamilies.Add(stimulus.Tags["score_family"]);
            tiers.Add(stimulus.Tags["tier"]);
            if (string.IsNullOrEmpty(stimulus.Tags["provenance_kind"]) ||
                string.IsNullOrEmpty(stimulus.Tags["provenance_source"]))
            {
                errors.Add($"{stimulus.Name}: provenance tags must be non-empty");
            }

            if (stimulus.RubricCount < 4)
            {
                errors.Add(
                    $"{stimulus.Name}: expected outcome plus at least three rubric items required");
            }

            var positiveIsValid = TryParseIndexes(
                stimulus,
                "positive_controls",
                errors,
                out var positive);
            var negativeIsValid = TryParseIndexes(
                stimulus,
                "negative_controls",
                errors,
                out var negative);
            if (!positiveIsValid || !negativeIsValid)
            {
                continue;
            }

            var validIndexes = Enumerable
                .Range(0, Math.Max(0, stimulus.RubricCount - 1))
                .ToHashSet();
            if (positive.Count == 0 || negative.Count == 0)
            {
                errors.Add(
                    $"{stimulus.Name}: positive and negative controls must be non-empty");
            }

            if (positive.Overlaps(negative))
            {
                errors.Add($"{stimulus.Name}: control indexes overlap");
            }

            if (!positive.Concat(negative).All(validIndexes.Contains))
            {
                errors.Add($"{stimulus.Name}: control index is out of range");
            }

            var fixtureDirectory = Path.GetDirectoryName(layout.VallyPath)!;
            foreach (var fixtureSource in stimulus.FixtureSources)
            {
                if (!File.Exists(Path.Combine(fixtureDirectory, fixtureSource)))
                {
                    errors.Add(
                        $"{stimulus.Name}: missing fixture source {fixtureSource}");
                }
            }
        }

        ValidateArchitecturePortability(stimuli, errors);

        var duplicateIds = evalIds
            .GroupBy(identifier => identifier, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (duplicateIds.Length > 0)
        {
            errors.Add($"Duplicate Vally eval IDs: {string.Join(", ", duplicateIds)}");
        }

        if (!coveredPrefixes.SetEquals(prefixes))
        {
            errors.Add(
                "Vally coverage differs from requirement prefixes: missing " +
                $"{Join(prefixes.Except(coveredPrefixes))}, extra " +
                Join(coveredPrefixes.Except(prefixes)));
        }

        if (!tiers.IsSupersetOf(["train", "held-out"]))
        {
            errors.Add("Vally suite requires both train and held-out cases");
        }

        if (!scoreFamilies.IsSupersetOf(["scope-control", "no-defect-control"]))
        {
            errors.Add(
                "Vally suite requires scope-control and no-defect-control canaries");
        }
    }

    private static void ValidateWiring(
        SkillLayout layout,
        ICollection<string> errors)
    {
        var skill = File.ReadAllText(layout.SkillPath, Encoding.UTF8);
        var report = File.ReadAllText(layout.ReportTemplatePath, Encoding.UTF8);
        foreach (var reference in new[]
        {
            "references/areas/index.md",
            "references/artifact-acquisition.md",
            "references/feedback.md",
            "references/overlays/",
            "references/status-boundaries.md",
            "references/targeted-profiles.md",
            "eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj",
            "eng/skill-evals/blazor-component-readiness/regression.vally.yaml",
        })
        {
            if (!skill.Contains(reference, StringComparison.Ordinal))
            {
                errors.Add($"SKILL.md does not reference {reference}");
            }
        }

        var evalPolicy = File.ReadAllText(layout.EvalPolicyPath, Encoding.UTF8);
        if (!evalPolicy.Contains(
            "eng/skill-evals/blazor-component-readiness/regression.vally.yaml",
            StringComparison.Ordinal))
        {
            errors.Add(
                "Evaluation policy does not reference the specialized repository eval suite");
        }

        foreach (var heading in new[]
        {
            "Requirement ID",
            "Requirement scope",
            "Reviewer follow-up",
        })
        {
            if (!report.Contains(heading, StringComparison.Ordinal))
            {
                errors.Add($"Report template is missing scorecard column {heading}");
            }
        }

        foreach (var marker in new[]
        {
            "--evidence-bundle",
            "--legacy-evidence",
            "receipt validate",
            "EV1-",
        })
        {
            if (!skill.Contains(marker, StringComparison.Ordinal))
            {
                errors.Add($"SKILL.md is missing stable evidence marker {marker}");
            }

            if (!report.Contains(marker, StringComparison.Ordinal))
            {
                errors.Add($"Report template is missing stable evidence marker {marker}");
            }
        }

        var vally = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        if (!vally.Contains("eval-21-stable-evidence-integrity", StringComparison.Ordinal))
        {
            errors.Add("Vally suite is missing stable evidence integrity regression");
        }

        if (!vally.Contains(
            "eval-22-shared-projection-and-provenance",
            StringComparison.Ordinal))
        {
            errors.Add(
                "Vally suite is missing shared projection and provenance regression");
        }

    }

    private static void ValidateArchitecturePortability(
        IReadOnlyList<VallyStimulus> stimuli,
        ICollection<string> errors)
    {
        const string Name = "eval-23-architecture-portability";
        var matches = stimuli
            .Where(candidate =>
                string.Equals(candidate.Name, Name, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length == 0)
        {
            errors.Add(
                "Vally suite is missing architecture portability regression");
            return;
        }

        if (matches.Length > 1)
        {
            errors.Add(
                $"Vally suite contains duplicate stimulus name '{Name}'; " +
                $"found {matches.Length}");
            return;
        }

        var stimulus = matches[0];
        if (!ContainsAll(
                stimulus.Prompt,
                "clearly fictional",
                "handwritten standalone date selector",
                "no generator or shared component runtime",
                "without importing architecture assumptions or evidence from unrelated components"))
        {
            errors.Add(
                $"{Name}: prompt no longer exercises a synthetic handwritten " +
                "standalone component without unrelated architecture or evidence assumptions");
        }

        if (!stimulus.Tags.ContainsKey("positive_controls") ||
            !stimulus.Tags.ContainsKey("negative_controls") ||
            !TryParseIndexes(
                stimulus,
                "positive_controls",
                [],
                out var positive) ||
            !TryParseIndexes(
                stimulus,
                "negative_controls",
                [],
                out var negative))
        {
            return;
        }

        var maximumIndex = stimulus.RubricItems.Count - 2;
        if (positive.Concat(negative).Any(index =>
            index < 0 || index > maximumIndex))
        {
            return;
        }

        var positiveItems = SelectControlledRubricItems(stimulus, positive);
        if (!positiveItems.Any(item =>
                ContainsClause(
                    item,
                    "The target remains one handwritten standalone date selector") &&
                ContainsClause(
                    item,
                    "generated ownership and a shared component runtime are not assumed")) ||
            !positiveItems.Any(item => ContainsClause(
                item,
                "Artifact acquisition uses the target's configured public source and " +
                "binds the exact package ID, version, digest, and repository snapshot " +
                "rather than borrowing another suite's feed or package records")) ||
            !positiveItems.Any(item => ContainsClause(
                item,
                "The same bundled core, applicable overlays, exact status vocabulary, " +
                "and evidence hierarchy apply without changing the rubric for the new architecture")))
        {
            errors.Add(
                $"{Name}: positive-controlled rubric items no longer affirm the " +
                "handwritten standalone acquisition/binding, rubric, status, and evidence direction");
        }

        var negativeItems = SelectControlledRubricItems(stimulus, negative);
        if (!negativeItems.Any(item => ContainsClause(
                item,
                "A response that assumes generated wrappers, a shared runtime, private feeds, " +
                "commercial release machinery, or unrelated-component evidence fails this portability case")))
        {
            errors.Add(
                $"{Name}: negative-controlled rubric items no longer state that " +
                "assuming generated, shared-runtime, commercial-release, or " +
                "unrelated-component facts fails the portability case");
        }
    }

    private static string[] SelectControlledRubricItems(
        VallyStimulus stimulus,
        IEnumerable<int> indexes)
    {
        return indexes
            .Select(index => stimulus.RubricItems[index + 1])
            .ToArray();
    }

    private static bool ContainsClause(string content, string clause)
    {
        return content.Contains(clause, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAll(string content, params string[] values)
    {
        return values.All(value =>
            content.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryParseIndexes(
        VallyStimulus stimulus,
        string tagName,
        ICollection<string> errors,
        out HashSet<int> indexes)
    {
        indexes = [];
        var value = stimulus.Tags[tagName];
        if (value.Length == 0)
        {
            return true;
        }

        foreach (var item in value.Split(',', StringSplitOptions.TrimEntries))
        {
            if (item.Length == 0 ||
                !int.TryParse(
                    item,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var index))
            {
                errors.Add(
                    $"{stimulus.Name}: {tagName} contains invalid control index " +
                    $"'{item}'; expected a non-negative Int32");
                return false;
            }

            indexes.Add(index);
        }

        return true;
    }

    private static IReadOnlyDictionary<string, string> ParseTags(
        string stimulusName,
        string block)
    {
        var lines = new List<string>();
        using (var reader = new StringReader(block))
        {
            while (reader.ReadLine() is { } line)
            {
                lines.Add(line);
            }
        }

        var markers = FindStimulusProperties(lines, stimulusName, "tags");
        if (markers.Length != 1)
        {
            throw new InvalidDataException(
                $"{stimulusName}: expected exactly one stimulus-level Vally tags " +
                $"mapping; found {markers.Length}");
        }

        var tags = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = markers[0] + 1; index < lines.Count; index++)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith("      ", StringComparison.Ordinal))
            {
                break;
            }

            var tag = TagPattern.Match(line);
            if (!tag.Success)
            {
                continue;
            }

            var name = tag.Groups[1].Value;
            if (!tags.TryAdd(name, tag.Groups[2].Value))
            {
                throw new InvalidDataException(
                    $"{stimulusName}: duplicate Vally tag {name}");
            }
        }

        return tags;
    }

    private static int[] FindStimulusProperties(
        IReadOnlyList<string> lines,
        string stimulusName,
        string propertyName)
    {
        var matches = new List<int>();
        var promptCount = 0;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var property = StimulusPropertyPattern.Match(line);
            if (!property.Success)
            {
                continue;
            }

            var name = property.Groups[1].Value;
            var value = property.Groups[2].Value;
            if (name == "prompt")
            {
                promptCount++;
                if (value != "|-")
                {
                    throw new InvalidDataException(
                        $"{stimulusName}: prompt must use the supported " +
                        "'prompt: |-' block scalar form");
                }

                while (index + 1 < lines.Count &&
                    (string.IsNullOrWhiteSpace(lines[index + 1]) ||
                    CountIndent(lines[index + 1]) > 4))
                {
                    index++;
                }

                continue;
            }

            if (name == propertyName &&
                (value.Length == 0 || value.StartsWith('#')))
            {
                matches.Add(index);
            }
        }

        if (promptCount != 1)
        {
            throw new InvalidDataException(
                $"{stimulusName}: expected exactly one supported prompt block; " +
                $"found {promptCount}");
        }

        return matches.ToArray();
    }

    private static int CountIndent(string line)
    {
        var index = 0;
        while (index < line.Length && line[index] == ' ')
        {
            index++;
        }

        return index;
    }

    private static string Prefix(string identifier)
    {
        return identifier[..identifier.LastIndexOf('-')];
    }

    private static int Number(string identifier)
    {
        return int.Parse(
            identifier[(identifier.LastIndexOf('-') + 1)..],
            CultureInfo.InvariantCulture);
    }

    private static string Join(IEnumerable<string> values)
    {
        return string.Join(", ", values.Order(StringComparer.Ordinal));
    }
}
