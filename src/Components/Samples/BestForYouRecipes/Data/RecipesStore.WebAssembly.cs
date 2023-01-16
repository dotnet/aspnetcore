// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace BestForYouRecipes;

public class RecipesStore : IRecipesStore
{
    private readonly HttpClient _http;

    public RecipesStore(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> AddImage(Stream imageData)
    {
        var response = await _http.PostAsync("api/images", new StreamContent(imageData));
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> AddRecipe(Recipe recipe)
    {
        var response = await _http.PostAsJsonAsync("api/recipes", recipe);
        return await response.Content.ReadAsStringAsync();
    }

    public Task<IEnumerable<Recipe>> GetRecipes(string? query)
        => throw new NotImplementedException();

    public Task<Recipe?> GetRecipe(string id)
        => throw new NotImplementedException();

    public Task<Recipe> UpdateRecipe(Recipe recipe)
        => throw new NotImplementedException();

    public Task<byte[]> GetImage(string filename)
        => throw new NotImplementedException();
}
