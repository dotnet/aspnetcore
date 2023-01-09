// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BestForYouRecipes;

public class Recipe
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Source { get; set; } = default!;
    public string SourceShort => Uri.TryCreate(Source, UriKind.Absolute, out var sourceUri) ? sourceUri.Authority : Source;
    public int PrepTime { get; set; }
    public int WaitTime { get; set; }
    public int CookTime { get; set; }
    public int Servings { get; set; }
    public string? Comments { get; set; }
    public IList<Review> Reviews { get; set; } = new List<Review>();
    public string Instructions { get; set; } = default!;
    public string[] Ingredients { get; set; } = default!;
    public string[] Tags { get; set; } = default!;
    public Uri CardImageUrl => new Uri($"images/cards/{Name}.png", UriKind.Relative);
    public Uri BannerImageUrl => new Uri($"images/banners/{Name} Banner.png", UriKind.Relative);
}
