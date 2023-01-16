// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BestForYouRecipes;

public class Recipe
{
    public string Id { get; set; } = default!;

    [Required]
    public string Name { get; set; } = default!;

    public string Source { get; set; } = default!;
    public string SourceShort => Uri.TryCreate(Source, UriKind.Absolute, out var sourceUri) ? sourceUri.Authority : Source;
    public int PrepTime { get; set; }
    public int WaitTime { get; set; }
    public int CookTime { get; set; }

    [Range(1, 100)]
    public int Servings { get; set; }

    public string? Comments { get; set; }
    public IList<Review> Reviews { get; set; } = new List<Review>();

    [Required]
    public string Instructions { get; set; } = default!;

    [Required(ErrorMessage = "Please add your ingredients"), MinLength(1, ErrorMessage = "Please add your ingredients")]
    public string[] Ingredients { get; set; } = default!;

    public string[] Tags { get; set; } = default!;

    [Required(ErrorMessage = "Please add a picture")]
    public string? ImageUrl { get; set; }

    public Uri CardImageUrl => new Uri(ImageUrl ?? $"images/cards/{Name}.png", UriKind.Relative);
    public Uri BannerImageUrl => new Uri(ImageUrl ?? $"images/banners/{Name} Banner.png", UriKind.Relative);
}
