// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace BestForYouRecipes;

public class RecipesStore : IRecipesStore
{
    IDictionary<string, Recipe> recipes;
    ConcurrentDictionary<string, byte[]> images = new();
    InMemorySearchProvider searchProvider;

    public RecipesStore()
    {
        var jsonPath = Path.Combine(Path.GetDirectoryName(typeof(RecipesStore).Assembly.Location)!, "Data", "recipes.json");
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
        await Task.Delay(1000);

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

    public Task<string> AddRecipe(Recipe recipe)
    {
        recipe.Id = recipes.Count.ToString(CultureInfo.InvariantCulture);
        recipes.Add(recipe.Id, recipe);
        return Task.FromResult(recipe.Id);
    }

    public async Task<string> AddImage(Stream imageData)
    {
        using var ms = new MemoryStream();
        await imageData.CopyToAsync(ms);

        var filename = Guid.NewGuid().ToString();
        images[filename] = ms.ToArray();
        return $"images/uploaded/{filename}";
    }

    public Task<byte[]> GetImage(string filename)
        => Task.FromResult(images[filename]);
}
