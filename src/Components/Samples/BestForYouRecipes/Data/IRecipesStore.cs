// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BestForYouRecipes;

public interface IRecipesStore
{
    Task<IEnumerable<Recipe>> GetRecipes(string? query);

    Task<Recipe?> GetRecipe(string id);

    Task<Recipe> UpdateRecipe(Recipe recipe);

    Task<byte[]> GetImage(string filename);

    Task<string> AddRecipe(Recipe recipe);

    Task<string> AddImage(Stream imageData);
}
