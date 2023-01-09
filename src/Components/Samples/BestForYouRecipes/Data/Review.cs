// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BestForYouRecipes;

public class Review
{
    [Required]
    [Range(1, 5)]
    public double Rating { get; set; }

    [Required]
    [StringLength(50, ErrorMessage = "Text must be no more than 50 characters.")]
    public string? Text { get; set; }
}
