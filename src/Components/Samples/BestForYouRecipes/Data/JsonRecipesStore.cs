// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace BestForYouRecipes;

public class JsonRecipesStore : IRecipesStore
{
    IDictionary<string, Recipe> recipes;
    InMemorySearchProvider searchProvider;

    public JsonRecipesStore()
    {
        var jsonPath = Path.Combine(Path.GetDirectoryName(typeof(JsonRecipesStore).Assembly.Location)!, "Data", "recipes.json");
        var json = File.ReadAllText(jsonPath);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        recipes = JsonSerializer.Deserialize<Dictionary<string, Recipe>>(json, jsonOptions)!;
        searchProvider = new InMemorySearchProvider(recipes);
    }

    public async Task<IEnumerable<Recipe>> GetRecipes(string? query)
    {
        // Simulate DB slowness
        await Task.Delay(500);

        return string.IsNullOrWhiteSpace(query)
            ? recipes.Values
            : searchProvider.Search(query);
    }

    public Task<Recipe?> GetRecipe(string id)
    {
        recipes.TryGetValue(id, out var recipe);
        return Task.FromResult(recipe);
    }

    public Task<Recipe> UpdateRecipe(Recipe recipe)
    {
        return Task.FromResult(recipe);
    }
}
