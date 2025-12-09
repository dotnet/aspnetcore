// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Http.Features;

public class FeatureCollectionExtensionsTests
{
    [Fact]
    public void AddedFeatureGetsReturned()
    {
        // Arrange
        var features = new FeatureCollection();
        var thing = new Thing();
        features.Set<IThing>(thing);

        // Act
        var retrievedThing = features.GetRequiredFeature<IThing>();

        // Assert
        Assert.NotNull(retrievedThing);
        Assert.Equal(retrievedThing, thing);
    }

    [Fact]
    public void ExceptionThrown_WhenAskedForUnknownFeature()
    {
        // Arrange
        var features = new FeatureCollection();
        var thing = new Thing();
        features.Set<IThing>(thing);

        // Assert
        Assert.Throws<InvalidOperationException>(() => features.GetRequiredFeature<object>());
    }
}
