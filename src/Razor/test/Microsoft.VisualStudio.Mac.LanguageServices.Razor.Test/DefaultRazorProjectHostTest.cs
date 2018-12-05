// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using MonoDevelop.Projects.MSBuild;
using Xunit;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.ProjectSystem
{
    public class DefaultRazorProjectHostTest
    {
        [Fact]
        public void TryGetDefaultConfiguration_FailsIfNoConfiguration()
        {
            // Arrange
            var projectProperties = new MSBuildPropertyGroup();

            // Act
            var result = DefaultRazorProjectHost.TryGetDefaultConfiguration(projectProperties, out var defaultConfiguration);

            // Assert
            Assert.False(result);
            Assert.Null(defaultConfiguration);
        }

        [Fact]
        public void TryGetDefaultConfiguration_FailsIfEmptyConfiguration()
        {
            // Arrange
            var projectProperties = new MSBuildPropertyGroup();
            projectProperties.SetValue("RazorDefaultConfiguration", string.Empty);

            // Act
            var result = DefaultRazorProjectHost.TryGetDefaultConfiguration(projectProperties, out var defaultConfiguration);

            // Assert
            Assert.False(result);
            Assert.Null(defaultConfiguration);
        }

        [Fact]
        public void TryGetDefaultConfiguration_SucceedsWithValidConfiguration()
        {
            // Arrange
            var expectedConfiguration = "Razor-13.37";
            var projectProperties = new MSBuildPropertyGroup();
            projectProperties.SetValue("RazorDefaultConfiguration", expectedConfiguration);

            // Act
            var result = DefaultRazorProjectHost.TryGetDefaultConfiguration(projectProperties, out var defaultConfiguration);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedConfiguration, defaultConfiguration);
        }

        [Fact]
        public void TryGetLanguageVersion_FailsIfNoLanguageVersion()
        {
            // Arrange
            var projectProperties = new MSBuildPropertyGroup();

            // Act
            var result = DefaultRazorProjectHost.TryGetLanguageVersion(projectProperties, out var languageVersion);

            // Assert
            Assert.False(result);
            Assert.Null(languageVersion);
        }

        [Fact]
        public void TryGetLanguageVersion_FailsIfEmptyLanguageVersion()
        {
            // Arrange
            var projectProperties = new MSBuildPropertyGroup();
            projectProperties.SetValue("RazorLangVersion", string.Empty);

            // Act
            var result = DefaultRazorProjectHost.TryGetLanguageVersion(projectProperties, out var languageVersion);

            // Assert
            Assert.False(result);
            Assert.Null(languageVersion);
        }

        [Fact]
        public void TryGetLanguageVersion_SucceedsWithValidLanguageVersion()
        {
            // Arrange
            var projectProperties = new MSBuildPropertyGroup();
            projectProperties.SetValue("RazorLangVersion", "1.0");

            // Act
            var result = DefaultRazorProjectHost.TryGetLanguageVersion(projectProperties, out var languageVersion);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Version_1_0, languageVersion);
        }

        [Fact]
        public void TryGetLanguageVersion_SucceedsWithUnknownLanguageVersion_DefaultsToLatest()
        {
            // Arrange
            var projectProperties = new MSBuildPropertyGroup();
            projectProperties.SetValue("RazorLangVersion", "13.37");

            // Act
            var result = DefaultRazorProjectHost.TryGetLanguageVersion(projectProperties, out var languageVersion);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Latest, languageVersion);
        }

        [Fact]
        public void TryGetConfigurationItem_FailsNoRazorConfigurationItems()
        {
            // Arrange
            var projectItems = Enumerable.Empty<IMSBuildItemEvaluated>();

            // Act
            var result = DefaultRazorProjectHost.TryGetConfigurationItem("Razor-13.37", projectItems, out var configurationItem);

            // Assert
            Assert.False(result);
            Assert.Null(configurationItem);
        }

        [Fact]
        public void TryGetConfigurationItem_FailsNoMatchingRazorConfigurationItems()
        {
            // Arrange
            var projectItems = new IMSBuildItemEvaluated[]
            {
                new TestMSBuildItem("RazorConfiguration")
                {
                    Include = "Razor-10.0",
                }
            };

            // Act
            var result = DefaultRazorProjectHost.TryGetConfigurationItem("Razor-13.37", projectItems, out var configurationItem);

            // Assert
            Assert.False(result);
            Assert.Null(configurationItem);
        }

        [Fact]
        public void TryGetConfigurationItem_SucceedsForMatchingConfigurationItem()
        {
            // Arrange
            var expectedConfiguration = "Razor-13.37";
            var expectedConfigurationItem = new TestMSBuildItem("RazorConfiguration")
            {
                Include = expectedConfiguration,
            };
            var projectItems = new IMSBuildItemEvaluated[]
            {
                new TestMSBuildItem("RazorConfiguration")
                {
                    Include = "Razor-10.0-DoesNotMatch",
                },
                expectedConfigurationItem
            };

            // Act
            var result = DefaultRazorProjectHost.TryGetConfigurationItem(expectedConfiguration, projectItems, out var configurationItem);

            // Assert
            Assert.True(result);
            Assert.Same(expectedConfigurationItem, configurationItem);
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_FailsIfNoExtensions()
        {
            // Arrange
            var configurationItem = new TestMSBuildItem("RazorConfiguration");

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguredExtensionNames(configurationItem, out var configuredExtensionnames);

            // Assert
            Assert.False(result);
            Assert.Null(configuredExtensionnames);
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_FailsIfEmptyExtensions()
        {
            // Arrange
            var configurationItem = new TestMSBuildItem("RazorConfiguration");
            configurationItem.TestMetadata.SetValue("Extensions", string.Empty);

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguredExtensionNames(configurationItem, out var configuredExtensionNames);

            // Assert
            Assert.False(result);
            Assert.Null(configuredExtensionNames);
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_SucceedsIfSingleExtension()
        {
            // Arrange
            var expectedExtensionName = "SomeExtensionName";
            var configurationItem = new TestMSBuildItem("RazorConfiguration");
            configurationItem.TestMetadata.SetValue("Extensions", expectedExtensionName);

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguredExtensionNames(configurationItem, out var configuredExtensionNames);

            // Assert
            Assert.True(result);
            var extensionName = Assert.Single(configuredExtensionNames);
            Assert.Equal(expectedExtensionName, extensionName);
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_SucceedsIfMultipleExtensions()
        {
            // Arrange
            var configurationItem = new TestMSBuildItem("RazorConfiguration");
            configurationItem.TestMetadata.SetValue("Extensions", "SomeExtensionName;SomeOtherExtensionName");

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguredExtensionNames(configurationItem, out var configuredExtensionNames);

            // Assert
            Assert.True(result);
            Assert.Collection(
                configuredExtensionNames,
                name => Assert.Equal("SomeExtensionName", name),
                name => Assert.Equal("SomeOtherExtensionName", name));
        }

        [Fact]
        public void GetExtensions_NoExtensionTypes_ReturnsEmptyArray()
        {
            // Arrange
            var projectItems = new IMSBuildItemEvaluated[]
            {
                new TestMSBuildItem("NotAnExtension")
                {
                    Include = "Extension1",
                },
            };

            // Act
            var extensions = DefaultRazorProjectHost.GetExtensions(new[] { "Extension1", "Extension2" }, projectItems);

            // Assert
            Assert.Empty(extensions);
        }

        [Fact]
        public void GetExtensions_UnConfiguredExtensionTypes_ReturnsEmptyArray()
        {
            // Arrange
            var projectItems = new IMSBuildItemEvaluated[]
            {
                new TestMSBuildItem("RazorExtension")
                {
                    Include = "UnconfiguredExtensionName",
                },
            };

            // Act
            var extensions = DefaultRazorProjectHost.GetExtensions(new[] { "Extension1", "Extension2" }, projectItems);

            // Assert
            Assert.Empty(extensions);
        }

        [Fact]
        public void GetExtensions_SomeConfiguredExtensions_ReturnsConfiguredExtensions()
        {
            // Arrange
            var expectedExtension1Name = "Extension1";
            var expectedExtension2Name = "Extension2";
            var projectItems = new IMSBuildItemEvaluated[]
            {
                new TestMSBuildItem("RazorExtension")
                {
                    Include = "UnconfiguredExtensionName",
                },
                new TestMSBuildItem("RazorExtension")
                {
                    Include = expectedExtension1Name,
                },
                new TestMSBuildItem("RazorExtension")
                {
                    Include = expectedExtension2Name,
                },
            };

            // Act
            var extensions = DefaultRazorProjectHost.GetExtensions(new[] { expectedExtension1Name, expectedExtension2Name }, projectItems);

            // Assert
            Assert.Collection(
                extensions,
                extension => Assert.Equal(expectedExtension1Name, extension.ExtensionName),
                extension => Assert.Equal(expectedExtension2Name, extension.ExtensionName));
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoDefaultConfiguration()
        {
            // Arrange
            var projectProperties = new MSBuildPropertyGroup();
            var projectItems = new IMSBuildItemEvaluated[0];

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguration(projectProperties, projectItems, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoLanguageVersion()
        {
            // Arrange
            var projectProperties = new MSBuildPropertyGroup();
            projectProperties.SetValue("RazorDefaultConfiguration", "Razor-13.37");
            var projectItems = new IMSBuildItemEvaluated[0];

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguration(projectProperties, projectItems, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoConfigurationItems()
        {
            // Arrange
            var projectProperties = new MSBuildPropertyGroup();
            projectProperties.SetValue("RazorDefaultConfiguration", "Razor-13.37");
            projectProperties.SetValue("RazorLangVersion", "1.0");
            var projectItems = new IMSBuildItemEvaluated[0];

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguration(projectProperties, projectItems, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoConfiguredExtensionNames()
        {
            // Arrange
            var projectProperties = new MSBuildPropertyGroup();
            projectProperties.SetValue("RazorDefaultConfiguration", "Razor-13.37");
            projectProperties.SetValue("RazorLangVersion", "1.0");
            var projectItems = new IMSBuildItemEvaluated[]
            {
                new TestMSBuildItem("RazorConfiguration")
                {
                    Include = "Razor-13.37",
                },
            };

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguration(projectProperties, projectItems, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        // This is more of an integration test but is here to test the overall flow/functionality
        [Fact]
        public void TryGetConfiguration_SucceedsWithAllPreRequisites()
        {
            // Arrange
            var expectedLanguageVersion = RazorLanguageVersion.Version_1_0;
            var expectedConfigurationName = "Razor-Test";
            var expectedExtension1Name = "Extension1";
            var expectedExtension2Name = "Extension2";
            var expectedRazorConfigurationItem = new TestMSBuildItem("RazorConfiguration")
            {
                Include = expectedConfigurationName,
            };
            expectedRazorConfigurationItem.TestMetadata.SetValue("Extensions", "Extension1;Extension2");
            var projectItems = new IMSBuildItemEvaluated[]
            {
                new TestMSBuildItem("RazorConfiguration")
                {
                    Include = "UnconfiguredRazorConfiguration",
                },
                new TestMSBuildItem("RazorExtension")
                {
                    Include = "UnconfiguredExtensionName",
                },
                new TestMSBuildItem("RazorExtension")
                {
                    Include = expectedExtension1Name,
                },
                new TestMSBuildItem("RazorExtension")
                {
                    Include = expectedExtension2Name,
                },
                expectedRazorConfigurationItem,
            };
            var projectProperties = new MSBuildPropertyGroup();
            projectProperties.SetValue("RazorDefaultConfiguration", expectedConfigurationName);
            projectProperties.SetValue("RazorLangVersion", "1.0");

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguration(projectProperties, projectItems, out var configuration);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedLanguageVersion, configuration.LanguageVersion);
            Assert.Equal(expectedConfigurationName, configuration.ConfigurationName);
            Assert.Collection(
                configuration.Extensions,
                extension => Assert.Equal(expectedExtension1Name, extension.ExtensionName),
                extension => Assert.Equal(expectedExtension2Name, extension.ExtensionName));
        }

        private class TestMSBuildItem : IMSBuildItemEvaluated
        {
            private readonly MSBuildPropertyGroup _metadata;
            private readonly string _name;
            private string _include;

            public TestMSBuildItem(string name)
            {
                _name = name;
                _metadata = new MSBuildPropertyGroup();
            }

            public string Name => _name;

            public string Include
            {
                get => _include;
                set => _include = value;
            }

            public MSBuildPropertyGroup TestMetadata => _metadata;

            public IMSBuildPropertyGroupEvaluated Metadata => _metadata;

            public string Condition => throw new System.NotImplementedException();

            public bool IsImported => throw new System.NotImplementedException();


            public string UnevaluatedInclude => throw new System.NotImplementedException();

            public MSBuildItem SourceItem => throw new System.NotImplementedException();

            public IEnumerable<MSBuildItem> SourceItems => throw new System.NotImplementedException();
        }
    }
}
