// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorParserFeatureFlagsTest
{
    [Fact]
    public void Create_LatestVersion_AllowsLatestFeatures()
    {
        // Arrange & Act
        var context = RazorParserFeatureFlags.Create(RazorLanguageVersion.Latest, FileKinds.Legacy);

        // Assert
        Assert.True(context.AllowComponentFileKind);
        Assert.True(context.AllowRazorInAllCodeBlocks);
        Assert.True(context.AllowUsingVariableDeclarations);
        Assert.True(context.AllowNullableForgivenessOperator);
    }

    [Fact]
    public void Create_21Version_Allows21Features()
    {
        // Arrange & Act
        var context = RazorParserFeatureFlags.Create(RazorLanguageVersion.Version_2_1, FileKinds.Legacy);

        // Assert
        Assert.True(context.AllowMinimizedBooleanTagHelperAttributes);
        Assert.True(context.AllowHtmlCommentsInTagHelpers);
    }

    [Fact]
    public void Create_OldestVersion_DoesNotAllowLatestFeatures()
    {
        // Arrange & Act
        var context = RazorParserFeatureFlags.Create(RazorLanguageVersion.Version_1_0, FileKinds.Legacy);

        // Assert
        Assert.False(context.AllowMinimizedBooleanTagHelperAttributes);
        Assert.False(context.AllowHtmlCommentsInTagHelpers);
        Assert.False(context.AllowComponentFileKind);
        Assert.False(context.AllowRazorInAllCodeBlocks);
        Assert.False(context.AllowUsingVariableDeclarations);
        Assert.False(context.AllowNullableForgivenessOperator);
    }
}
