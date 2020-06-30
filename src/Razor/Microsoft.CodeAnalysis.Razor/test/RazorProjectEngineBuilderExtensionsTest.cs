// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;
using static Microsoft.CodeAnalysis.Razor.RazorProjectEngineBuilderExtensions;

namespace Microsoft.CodeAnalysis.Razor
{
    public class RazorProjectEngineBuilderExtensionsTest
    {
        [Fact]
        public void SetCSharpLanguageVersion_ResolvesNonNumericCSharpLangVersions()
        {
            // Arrange
            var csharpLanguageVersion = CSharp.LanguageVersion.Latest;

            // Act
            var projectEngine = RazorProjectEngine.Create(builder =>
            {
                builder.SetCSharpLanguageVersion(csharpLanguageVersion);
            });

            // Assert
            var feature = projectEngine.EngineFeatures.OfType<ConfigureParserForCSharpVersionFeature>().FirstOrDefault();
            Assert.NotNull(feature);
            Assert.NotEqual(csharpLanguageVersion, feature.CSharpLanguageVersion);
        }
    }
}
