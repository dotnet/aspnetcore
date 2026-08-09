// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace AIApp.Components.Scenarios.SharedState;

public sealed class RecipeState
{
    public Recipe Recipe { get; set; } = new();
}

public sealed class Recipe
{
    public string Title { get; set; } = "";

    [JsonPropertyName("skill_level")]
    public string SkillLevel { get; set; } = "";

    [JsonPropertyName("cooking_time")]
    public string CookingTime { get; set; } = "";

    [JsonPropertyName("special_preferences")]
    public List<string> SpecialPreferences { get; set; } = new();

    public List<Ingredient> Ingredients { get; set; } = new();

    public List<string> Instructions { get; set; } = new();
}

public sealed class Ingredient
{
    public string Icon { get; set; } = "";
    public string Name { get; set; } = "";
    public string Amount { get; set; } = "";
}
