// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace ConsoleValidationSample.Models;

/// <summary>
/// Represents a product review demonstrating validation on record types
/// with <see cref="Range"/>, <see cref="Display"/>, and nested record struct validation.
/// </summary>
[ValidatableType]
public record ProductReview(
    [Range(10, 100, ErrorMessage = "Custom message")]
    int Rating,

    [Range(10, 100), Display(Name = "RecommendationScore")]
    int RecommendationScore,

    ReviewerInfo Reviewer
);

/// <summary>
/// Represents reviewer information demonstrating validation on record struct types
/// with <see cref="Required"/> and <see cref="StringLength"/> attributes.
/// </summary>
public record struct ReviewerInfo([Required] string Name, [StringLength(10)] string? Comment);
