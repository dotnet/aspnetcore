// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class RazorParserFeatureFlagsTest
    {
        [Fact]
        public void Create_LatestVersion_AllowsMinimizedBooleanTagHelperAttributes()
        {
            // Arrange & Act
            var context = RazorParserFeatureFlags.Create(RazorLanguageVersion.Version_2_1);

            // Assert
            Assert.True(context.AllowMinimizedBooleanTagHelperAttributes);
            Assert.True(context.AllowHtmlCommentsInTagHelpers);
        }

        [Fact]
        public void Create_OlderVersion_DoesNotAllowMinimizedBooleanTagHelperAttributes()
        {
            // Arrange & Act
            var context = RazorParserFeatureFlags.Create(RazorLanguageVersion.Version_1_1);

            // Assert
            Assert.False(context.AllowMinimizedBooleanTagHelperAttributes);
            Assert.False(context.AllowHtmlCommentsInTagHelpers);
        }
    }
}