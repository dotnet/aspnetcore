// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DojoClient.Components.Scenarios.SharedState;

public sealed record RecipeState
{
    public Recipe Recipe { get; init; } = new();
}

public sealed record Recipe
{
    public string Title { get; init; } = "";

    [JsonPropertyName("skill_level")]
    public string SkillLevel { get; init; } = "";

    [JsonPropertyName("cooking_time")]
    public string CookingTime { get; init; } = "";

    [JsonPropertyName("special_preferences")]
    public List<string> SpecialPreferences { get; init; } = [];

    public List<Ingredient> Ingredients { get; init; } = [];

    public List<string> Instructions { get; init; } = [];
}

public sealed record Ingredient
{
    [Description("A single Unicode emoji grapheme.")]
    public string Icon { get; init; } = "";

    public string Name { get; init; } = "";

    public string Amount { get; init; } = "";
}
