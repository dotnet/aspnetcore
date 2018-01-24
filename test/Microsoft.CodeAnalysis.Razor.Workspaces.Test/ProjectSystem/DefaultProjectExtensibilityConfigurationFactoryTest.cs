// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultProjectExtensibilityConfigurationFactoryTest
    {
        public static TheoryData LanguageVersionMappingData
        {
            get
            {
                return new TheoryData<AssemblyIdentity, RazorLanguageVersion>
                {
                    { new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("1.0.0.0")), RazorLanguageVersion.Version_1_0 },
                    { new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("1.1.0.0")), RazorLanguageVersion.Version_1_1 },
                    { new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("2.0.0.0")), RazorLanguageVersion.Version_2_0 },
                    { new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("2.1.0.0")), RazorLanguageVersion.Version_2_1 },
                };
            }
        }

        [Theory]
        [MemberData(nameof(LanguageVersionMappingData))]
        public void GetLanguageVersion_MapsExactVersionsCorrectly(AssemblyIdentity assemblyIdentity, RazorLanguageVersion expectedVersion)
        {
            // Act
            var languageVersion = DefaultProjectExtensibilityConfigurationFactory.GetLanguageVersion(assemblyIdentity);

            // Assert
            Assert.Same(expectedVersion, languageVersion);
        }

        [Fact]
        public void GetLanguageVersion_MapsFuture_1_0_VersionsCorrectly()
        {
            // Arrange
            var assemblyIdentity = new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("1.3.0.0"));

            // Act
            var languageVersion = DefaultProjectExtensibilityConfigurationFactory.GetLanguageVersion(assemblyIdentity);

            // Assert
            Assert.Same(RazorLanguageVersion.Version_1_1, languageVersion);
        }

        [Fact]
        public void GetLanguageVersion_MapsFuture_2_0_VersionsCorrectly()
        {
            // Arrange
            var assemblyIdentity = new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("2.3.0.0"));

            // Act
            var languageVersion = DefaultProjectExtensibilityConfigurationFactory.GetLanguageVersion(assemblyIdentity);

            // Assert
            Assert.Same(RazorLanguageVersion.Latest, languageVersion);
        }

        [Theory]
        [InlineData("1.0.0.0", "1.0.0.0")]
        [InlineData("1.1.0.0", "1.1.0.0")]
        [InlineData("2.0.0.0", "2.0.0.0")]
        [InlineData("2.0.2.0", "2.0.2.0")]
        public void GetConfiguration_FindsSupportedConfiguration_ForNewRazor(string razorVersion, string mvcVersion)
        {
            // Arrange
            var references = new AssemblyIdentity[]
            {
                new AssemblyIdentity("Microsoft.AspNetCore.Razor.Language", new Version(razorVersion)),
                new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version(mvcVersion)),
            };

            var factory = new DefaultProjectExtensibilityConfigurationFactory();

            // Act
            var result = factory.GetConfiguration(references);

            // Assert
            var configuration = Assert.IsType<MvcExtensibilityConfiguration>(result);
            Assert.Equal(ProjectExtensibilityConfigurationKind.ApproximateMatch, configuration.Kind);
            Assert.Equal(razorVersion, configuration.RazorAssembly.Identity.Version.ToString());
            Assert.Equal(mvcVersion, configuration.MvcAssembly.Identity.Version.ToString());
        }

        [Theory]
        [InlineData("1.0.0.0", "1.0.0.0")]
        [InlineData("1.1.0.0", "1.1.0.0")]
        [InlineData("1.9.9.9", "2.0.0.0")] // MVC version is ignored
        public void GetConfiguration_FindsSupportedConfiguration_ForOldRazor(string razorVersion, string mvcVersion)
        {
            // Arrange
            var references = new AssemblyIdentity[]
            {
                new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version(razorVersion)),
                new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version(mvcVersion)),
            };

            var factory = new DefaultProjectExtensibilityConfigurationFactory();

            // Act
            var result = factory.GetConfiguration(references);

            // Assert
            var configuration = Assert.IsType<MvcExtensibilityConfiguration>(result);
            Assert.Equal(ProjectExtensibilityConfigurationKind.ApproximateMatch, configuration.Kind);
            Assert.Equal(razorVersion, configuration.RazorAssembly.Identity.Version.ToString());
            Assert.Equal(mvcVersion, configuration.MvcAssembly.Identity.Version.ToString());
        }

        [Fact]
        public void GetConfiguration_RazorVersion_NewAssemblyWinsOverOld()
        {
            // Arrange
            var references = new AssemblyIdentity[]
            {
                new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("1.0.0.0")),
                new AssemblyIdentity("Microsoft.AspNetCore.Razor.Language", new Version("2.0.0.0")),
                new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version("2.0.0.0")),
            };

            var factory = new DefaultProjectExtensibilityConfigurationFactory();

            // Act
            var result = factory.GetConfiguration(references);

            // Assert
            var configuration = Assert.IsType<MvcExtensibilityConfiguration>(result);
            Assert.Equal(ProjectExtensibilityConfigurationKind.ApproximateMatch, configuration.Kind);
            Assert.Equal("2.0.0.0", configuration.RazorAssembly.Identity.Version.ToString());
            Assert.Equal("2.0.0.0", configuration.MvcAssembly.Identity.Version.ToString());
        }

        [Fact]
        public void GetConfiguration_RazorVersion_OldAssemblyIgnoredPastV1()
        {
            // Arrange
            var references = new AssemblyIdentity[]
            {
                new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("2.0.0.0")),
                new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version("2.0.0.0")),
            };

            var factory = new DefaultProjectExtensibilityConfigurationFactory();

            // Act
            var result = factory.GetConfiguration(references);

            // Assert
            var configuration = Assert.IsType<MvcExtensibilityConfiguration>(result);
            Assert.Equal(ProjectExtensibilityConfigurationKind.Fallback, configuration.Kind);
            Assert.Equal("2.0.0.0", configuration.RazorAssembly.Identity.Version.ToString());
            Assert.Equal("2.0.0.0", configuration.MvcAssembly.Identity.Version.ToString());
        }

        [Fact]
        public void GetConfiguration_NoRazorVersion_ChoosesDefault()
        {
            // Arrange
            var references = new AssemblyIdentity[]
            {
                new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version("2.0.0.0")),
            };

            var factory = new DefaultProjectExtensibilityConfigurationFactory();

            // Act
            var result = factory.GetConfiguration(references);

            // Assert
            var configuration = Assert.IsType<MvcExtensibilityConfiguration>(result);
            Assert.Equal(ProjectExtensibilityConfigurationKind.Fallback, configuration.Kind);
            Assert.Equal("2.0.0.0", configuration.RazorAssembly.Identity.Version.ToString());
            Assert.Equal("2.0.0.0", configuration.MvcAssembly.Identity.Version.ToString());
        }

        [Fact]
        public void GetConfiguration_UnsupportedRazorVersion_ChoosesDefault()
        {
            // Arrange
            var references = new AssemblyIdentity[]
            {
                new AssemblyIdentity("Microsoft.AspNetCore.Razor.Language", new Version("3.0.0.0")),
                new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version("2.0.0.0")),
            };

            var factory = new DefaultProjectExtensibilityConfigurationFactory();

            // Act
            var result = factory.GetConfiguration(references);

            // Assert
            var configuration = Assert.IsType<MvcExtensibilityConfiguration>(result);
            Assert.Equal(ProjectExtensibilityConfigurationKind.Fallback, configuration.Kind);
            Assert.Equal("2.0.0.0", configuration.RazorAssembly.Identity.Version.ToString());
            Assert.Equal("2.0.0.0", configuration.MvcAssembly.Identity.Version.ToString());
        }

        [Fact]
        public void GetConfiguration_NoMvcVersion_ChoosesDefault()
        {
            // Arrange
            var references = new AssemblyIdentity[]
            {
                new AssemblyIdentity("Microsoft.AspNetCore.Razor.Language", new Version("2.0.0.0")),
            };

            var factory = new DefaultProjectExtensibilityConfigurationFactory();

            // Act
            var result = factory.GetConfiguration(references);

            // Assert
            var configuration = Assert.IsType<MvcExtensibilityConfiguration>(result);
            Assert.Equal(ProjectExtensibilityConfigurationKind.Fallback, configuration.Kind);
            Assert.Equal("2.0.0.0", configuration.RazorAssembly.Identity.Version.ToString());
            Assert.Equal("2.0.0.0", configuration.MvcAssembly.Identity.Version.ToString());
        }

        [Fact]
        public void GetConfiguration_UnsupportedMvcVersion_ChoosesDefault()
        {
            // Arrange
            var references = new AssemblyIdentity[]
            {
                new AssemblyIdentity("Microsoft.AspNetCore.Razor.Language", new Version("2.0.0.0")),
                new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version("3.0.0.0")),
            };

            var factory = new DefaultProjectExtensibilityConfigurationFactory();

            // Act
            var result = factory.GetConfiguration(references);

            // Assert
            var configuration = Assert.IsType<MvcExtensibilityConfiguration>(result);
            Assert.Equal(ProjectExtensibilityConfigurationKind.Fallback, configuration.Kind);
            Assert.Equal("2.0.0.0", configuration.RazorAssembly.Identity.Version.ToString());
            Assert.Equal("2.0.0.0", configuration.MvcAssembly.Identity.Version.ToString());
        }
    }
}
