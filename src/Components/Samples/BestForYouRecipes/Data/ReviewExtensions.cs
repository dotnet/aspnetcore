// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BestForYouRecipes;

public static class ReviewExtensions
{
    public static double AverageRating(this IList<Review> reviews)
        => Math.Round(reviews.Select(review => review.Rating).DefaultIfEmpty(0).Average(), 1);
}
