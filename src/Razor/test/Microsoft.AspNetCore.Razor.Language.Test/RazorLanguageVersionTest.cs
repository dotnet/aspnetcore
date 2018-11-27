// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class RazorLanguageVersionTest
    {
        [Fact]
        public void TryParseInvalid()
        {
            // Arrange
            var value = "not-version";

            // Act
            var result = RazorLanguageVersion.TryParse(value, out var _);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryParse10()
        {
            // Arrange
            var value = "1.0";

            // Act
            var result = RazorLanguageVersion.TryParse(value, out var version);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Version_1_0, version);
        }

        [Fact]
        public void TryParse11()
        {
            // Arrange
            var value = "1.1";

            // Act
            var result = RazorLanguageVersion.TryParse(value, out var version);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Version_1_1, version);
        }

        [Fact]
        public void TryParse20()
        {
            // Arrange
            var value = "2.0";

            // Act
            var result = RazorLanguageVersion.TryParse(value, out var version);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Version_2_0, version);
        }

        [Fact]
        public void TryParse21()
        {
            // Arrange
            var value = "2.1";

            // Act
            var result = RazorLanguageVersion.TryParse(value, out var version);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Version_2_1, version);
        }

        [Fact]
        public void TryParse30()
        {
            // Arrange
            var value = "3.0";

            // Act
            var result = RazorLanguageVersion.TryParse(value, out var version);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Version_3_0, version);
        }

        [Fact]
        public void TryParseLatest()
        {
            // Arrange
            var value = "Latest";

            // Act
            var result = RazorLanguageVersion.TryParse(value, out var version);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Version_3_0, version);
        }

        [Fact]
        public void TryParseExperimental()
        {
            // Arrange
            var value = "experimental";

            // Act
            var result = RazorLanguageVersion.TryParse(value, out var version);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Experimental, version);
        }

        [Fact]
        public void LatestPointsToNewestVersion()
        {
            // Arrange
            var v = RazorLanguageVersion.Parse("latest");
            var versions = typeof(RazorLanguageVersion).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.Name.StartsWith("Version_"))
                .Select(f => f.GetValue(obj: null))
                .Cast<RazorLanguageVersion>();

            // Act & Assert
            Assert.NotEmpty(versions);
            foreach (var version in versions)
            {
                Assert.True(version.CompareTo(v) <= 0, $"RazorLanguageVersion {version} has a higher version than RazorLanguageVersion.Latest");
            }
        }
    }
}
