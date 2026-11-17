// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace AGUIDojoApi.SharedState;

internal sealed class Recipe
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("skill_level")]
    public string SkillLevel { get; set; } = "";

    [JsonPropertyName("cooking_time")]
    public string CookingTime { get; set; } = "";

    [JsonPropertyName("special_preferences")]
    public List<string> SpecialPreferences { get; set; } = [];

    [JsonPropertyName("ingredients")]
    public List<Ingredient> Ingredients { get; set; } = [];

    [JsonPropertyName("instructions")]
    public List<string> Instructions { get; set; } = [];
}
