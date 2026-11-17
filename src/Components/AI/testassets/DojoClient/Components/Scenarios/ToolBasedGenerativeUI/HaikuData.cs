// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace DojoClient.Components.Scenarios.ToolBasedGenerativeUI;

public sealed class HaikuData
{
    internal const string AncientPondImage = "ancient-pond.svg";
    internal const string DefaultGradient = "linear-gradient(135deg, #667eea, #764ba2)";
    private static readonly JsonSerializerOptions s_jsonOptions =
        new(JsonSerializerDefaults.Web);
    private static readonly Regex s_safeGradientPattern = new(
        @"\Alinear-gradient\(\s*\d{1,3}deg\s*,\s*#[0-9a-fA-F]{3}(?:[0-9a-fA-F]{3})?\s*,\s*#[0-9a-fA-F]{3}(?:[0-9a-fA-F]{3})?\s*\)\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public List<string> Japanese { get; set; } = [];

    public List<string> English { get; set; } = [];

    public string? ImageName { get; set; }

    public string Gradient { get; set; } = "";

    internal static HaikuData Create(
        IEnumerable<string>? japanese,
        IEnumerable<string>? english,
        string? imageName,
        string? gradient)
        => new()
        {
            Japanese = japanese is null ? [] : [.. japanese],
            English = english is null ? [] : [.. english],
            ImageName = NormalizeImageName(imageName),
            Gradient = NormalizeGradient(gradient),
        };

    internal static HaikuData FromCall(FunctionCallContent call)
    {
        ArgumentNullException.ThrowIfNull(call);

        var arguments = JsonSerializer
            .SerializeToElement(call.Arguments, s_jsonOptions)
            .Deserialize<HaikuArguments>(s_jsonOptions);
        return Create(
            arguments?.Japanese,
            arguments?.English,
            arguments?.ImageName,
            arguments?.Gradient);
    }

    internal static string NormalizeGradient(string? gradient)
        => gradient is not null && s_safeGradientPattern.IsMatch(gradient)
            ? gradient
            : DefaultGradient;

    private static string? NormalizeImageName(string? imageName)
        => imageName == AncientPondImage ? imageName : null;

    private sealed class HaikuArguments
    {
        public List<string>? Japanese { get; set; }

        public List<string>? English { get; set; }

        [JsonPropertyName("image_name")]
        public string? ImageName { get; set; }

        public string? Gradient { get; set; }
    }
}
