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
            var context = RazorParserFeatureFlags.Create(RazorLanguageVersion.Version2_1);

            // Assert
            Assert.True(context.AllowMinimizedBooleanTagHelperAttributes);
        }

        [Fact]
        public void Create_OlderVersion_DoesNotAllowMinimizedBooleanTagHelperAttributes()
        {
            // Arrange & Act
            var context = RazorParserFeatureFlags.Create(RazorLanguageVersion.Version1_1);

            // Assert
            Assert.False(context.AllowMinimizedBooleanTagHelperAttributes);
        }

        [Fact]
        public void Create_UnknownVersion_Throws()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => RazorParserFeatureFlags.Create(0));

            Assert.Equal("Provided value for razor language version is unsupported or invalid: '0'.", exception.Message);
        }
    }
}
