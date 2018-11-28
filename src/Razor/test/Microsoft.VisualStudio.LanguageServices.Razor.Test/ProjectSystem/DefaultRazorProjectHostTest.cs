// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;
using Xunit;
using ItemCollection = Microsoft.VisualStudio.ProjectSystem.ItemCollection;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultRazorProjectHostTest : ForegroundDispatcherTestBase
    {
        public DefaultRazorProjectHostTest()
        {
            Workspace = new AdhocWorkspace();
            ProjectManager = new TestProjectSnapshotManager(Dispatcher, Workspace);

            ConfigurationItems = new ItemCollection(Rules.RazorConfiguration.SchemaName);
            ExtensionItems = new ItemCollection(Rules.RazorExtension.SchemaName);
            DocumentItems = new ItemCollection(Rules.RazorGenerateWithTargetPath.SchemaName);
            RazorGeneralProperties = new PropertyCollection(Rules.RazorGeneral.SchemaName);
        }

        private ItemCollection ConfigurationItems { get; }

        private ItemCollection ExtensionItems { get; }

        private ItemCollection DocumentItems { get; }

        private PropertyCollection RazorGeneralProperties { get; }

        private TestProjectSnapshotManager ProjectManager { get; }

        private Workspace Workspace { get; }

        [Fact]
        public void TryGetDefaultConfiguration_FailsIfNoRule()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>().ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetDefaultConfiguration(projectState, out var defaultConfiguration);

            // Assert
            Assert.False(result);
            Assert.Null(defaultConfiguration);
        }

        [Fact]
        public void TryGetDefaultConfiguration_FailsIfNoConfiguration()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(Rules.RazorGeneral.SchemaName, new Dictionary<string, string>())
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetDefaultConfiguration(projectState, out var defaultConfiguration);

            // Assert
            Assert.False(result);
            Assert.Null(defaultConfiguration);
        }

        [Fact]
        public void TryGetDefaultConfiguration_FailsIfEmptyConfiguration()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(
                    Rules.RazorGeneral.SchemaName, 
                    new Dictionary<string, string>()
                    {
                        [Rules.RazorGeneral.RazorDefaultConfigurationProperty] = string.Empty
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetDefaultConfiguration(projectState, out var defaultConfiguration);

            // Assert
            Assert.False(result);
            Assert.Null(defaultConfiguration);
        }

        [Fact]
        public void TryGetDefaultConfiguration_SucceedsWithValidConfiguration()
        {
            // Arrange
            var expectedConfiguration = "Razor-13.37";
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(
                    Rules.RazorGeneral.SchemaName, 
                    new Dictionary<string, string>()
                    {
                        [Rules.RazorGeneral.RazorDefaultConfigurationProperty] = expectedConfiguration
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetDefaultConfiguration(projectState, out var defaultConfiguration);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedConfiguration, defaultConfiguration);
        }

        [Fact]
        public void TryGetLanguageVersion_FailsIfNoRule()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>().ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetLanguageVersion(projectState, out var languageVersion);

            // Assert
            Assert.False(result);
            Assert.Null(languageVersion);
        }

        [Fact]
        public void TryGetLanguageVersion_FailsIfNoLanguageVersion()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(Rules.RazorGeneral.SchemaName, new Dictionary<string, string>())
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetLanguageVersion(projectState, out var languageVersion);

            // Assert
            Assert.False(result);
            Assert.Null(languageVersion);
        }

        [Fact]
        public void TryGetLanguageVersion_FailsIfEmptyLanguageVersion()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(
                    Rules.RazorGeneral.SchemaName, 
                    new Dictionary<string, string>()
                    {
                        [Rules.RazorGeneral.RazorLangVersionProperty] = string.Empty
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetLanguageVersion(projectState, out var languageVersion);

            // Assert
            Assert.False(result);
            Assert.Null(languageVersion);
        }

        [Fact]
        public void TryGetLanguageVersion_SucceedsWithValidLanguageVersion()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(
                    Rules.RazorGeneral.SchemaName, 
                    new Dictionary<string, string>()
                    {
                        [Rules.RazorGeneral.RazorLangVersionProperty] = "1.0"
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetLanguageVersion(projectState, out var languageVersion);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Version_1_0, languageVersion);
        }

        [Fact]
        public void TryGetLanguageVersion_SucceedsWithUnknownLanguageVersion_DefaultsToLatest()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(
                    Rules.RazorGeneral.SchemaName, 
                    new Dictionary<string, string>()
                    {
                        [Rules.RazorGeneral.RazorLangVersionProperty] = "13.37"
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetLanguageVersion(projectState, out var languageVersion);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Latest, languageVersion);
        }

        [Fact]
        public void TryGetConfigurationItem_FailsNoRazorConfigurationRule()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>().ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetConfigurationItem("Razor-13.37", projectState, out var configurationItem);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryGetConfigurationItem_FailsNoRazorConfigurationItems()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorConfiguration.SchemaName] = TestProjectRuleSnapshot.CreateItems(
                    Rules.RazorConfiguration.SchemaName, 
                    new Dictionary<string, Dictionary<string, string>>())
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetConfigurationItem("Razor-13.37", projectState, out var configurationItem);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryGetConfigurationItem_FailsNoMatchingRazorConfigurationItems()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorConfiguration.SchemaName] = TestProjectRuleSnapshot.CreateItems(
                    Rules.RazorConfiguration.SchemaName, 
                    new Dictionary<string, Dictionary<string, string>>()
                    {
                        ["Razor-10.0"] = new Dictionary<string, string>(),
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetConfigurationItem("Razor-13.37", projectState, out var configurationItem);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryGetConfigurationItem_SucceedsForMatchingConfigurationItem()
        {
            // Arrange
            var expectedConfiguration = "Razor-13.37";
            var expectedConfigurationValue = new Dictionary<string, string>()
            {
                [Rules.RazorConfiguration.ExtensionsProperty] = "SomeExtension"
            };
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorConfiguration.SchemaName] = TestProjectRuleSnapshot.CreateItems(
                    Rules.RazorConfiguration.SchemaName, 
                    new Dictionary<string, Dictionary<string, string>>()
                {
                    [expectedConfiguration] = expectedConfigurationValue
                })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetConfigurationItem(expectedConfiguration, projectState, out var configurationItem);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedConfiguration, configurationItem.Key);
            Assert.True(Enumerable.SequenceEqual(expectedConfigurationValue, configurationItem.Value));
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_FailsIfNoExtensions()
        {
            // Arrange
            var items = new ItemCollection(Rules.RazorConfiguration.SchemaName);
            items.Item("Test");

            var item = items.ToSnapshot().Items.Single();

            // Act
            var result = DefaultRazorProjectHost.TryGetExtensionNames(item, out var configuredExtensionnames);

            // Assert
            Assert.False(result);
            Assert.Null(configuredExtensionnames);
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_FailsIfEmptyExtensions()
        {
            // Arrange
            var items = new ItemCollection(Rules.RazorConfiguration.SchemaName);
            items.Item("Test");
            items.Property("Test", Rules.RazorConfiguration.ExtensionsProperty, string.Empty);

            var item = items.ToSnapshot().Items.Single();

            // Act
            var result = DefaultRazorProjectHost.TryGetExtensionNames(item, out var configuredExtensionNames);

            // Assert
            Assert.False(result);
            Assert.Null(configuredExtensionNames);
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_SucceedsIfSingleExtension()
        {
            // Arrange
            var expectedExtensionName = "SomeExtensionName";

            var items = new ItemCollection(Rules.RazorConfiguration.SchemaName);
            items.Item("Test");
            items.Property("Test", Rules.RazorConfiguration.ExtensionsProperty, "SomeExtensionName");

            var item = items.ToSnapshot().Items.Single();

            // Act
            var result = DefaultRazorProjectHost.TryGetExtensionNames(item, out var configuredExtensionNames);

            // Assert
            Assert.True(result);
            var extensionName = Assert.Single(configuredExtensionNames);
            Assert.Equal(expectedExtensionName, extensionName);
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_SucceedsIfMultipleExtensions()
        {
            // Arrange
            var items = new ItemCollection(Rules.RazorConfiguration.SchemaName);
            items.Item("Test");
            items.Property("Test", Rules.RazorConfiguration.ExtensionsProperty, "SomeExtensionName;SomeOtherExtensionName");

            var item = items.ToSnapshot().Items.Single();

            // Act
            var result = DefaultRazorProjectHost.TryGetExtensionNames(item, out var configuredExtensionNames);

            // Assert
            Assert.True(result);
            Assert.Collection(
                configuredExtensionNames,
                name => Assert.Equal("SomeExtensionName", name),
                name => Assert.Equal("SomeOtherExtensionName", name));
        }

        [Fact]
        public void TryGetExtensions_NoExtensions()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>().ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetExtensions(new[] { "Extension1", "Extension2" }, projectState, out var extensions);

            // Assert
            Assert.False(result);
            Assert.Null(extensions);
        }

        [Fact]
        public void TryGetExtensions_SucceedsWithUnConfiguredExtensionTypes()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorExtension.PrimaryDataSourceItemType] = TestProjectRuleSnapshot.CreateItems(
                    Rules.RazorExtension.PrimaryDataSourceItemType,
                    new Dictionary<string, Dictionary<string, string>>()
                    {
                        ["UnconfiguredExtensionName"] = new Dictionary<string, string>()
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetExtensions(new[] { "Extension1", "Extension2" }, projectState, out var extensions);

            // Assert
            Assert.True(result);
            Assert.Empty(extensions);
        }

        [Fact]
        public void TryGetExtensions_SucceedsWithSomeConfiguredExtensions()
        {
            // Arrange
            var expectedExtension1Name = "Extension1";
            var expectedExtension2Name = "Extension2";
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorExtension.PrimaryDataSourceItemType] = TestProjectRuleSnapshot.CreateItems(
                    Rules.RazorExtension.PrimaryDataSourceItemType,
                    new Dictionary<string, Dictionary<string, string>>()
                    {
                        ["UnconfiguredExtensionName"] = new Dictionary<string, string>(),
                        [expectedExtension1Name] = new Dictionary<string, string>(),
                        [expectedExtension2Name] = new Dictionary<string, string>(),
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetExtensions(new[] { expectedExtension1Name, expectedExtension2Name }, projectState, out var extensions);

            // Assert
            Assert.True(result);
            Assert.Collection(
                extensions,
                extension => Assert.Equal(expectedExtension2Name, extension.ExtensionName),
                extension => Assert.Equal(expectedExtension1Name, extension.ExtensionName));
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoDefaultConfiguration()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>().ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguration(projectState, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoLanguageVersion()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(
                    Rules.RazorGeneral.SchemaName,
                    new Dictionary<string, string>()
                    {
                        [Rules.RazorGeneral.RazorDefaultConfigurationProperty] = "13.37"
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguration(projectState, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoConfigurationItems()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(
                    Rules.RazorGeneral.SchemaName,
                    new Dictionary<string, string>()
                    {
                        [Rules.RazorGeneral.RazorDefaultConfigurationProperty] = "13.37",
                        [Rules.RazorGeneral.RazorLangVersionProperty] = "1.0",
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguration(projectState, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoConfiguredExtensionNames()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(
                    Rules.RazorGeneral.SchemaName,
                    new Dictionary<string, string>()
                    {
                        [Rules.RazorGeneral.RazorDefaultConfigurationProperty] = "13.37",
                        [Rules.RazorGeneral.RazorLangVersionProperty] = "1.0",
                    }),
                [Rules.RazorConfiguration.SchemaName] = TestProjectRuleSnapshot.CreateItems(
                    Rules.RazorConfiguration.SchemaName,
                    new Dictionary<string, Dictionary<string, string>>()
                    {
                        ["Razor-13.37"] = new Dictionary<string, string>()
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguration(projectState, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoExtensions()
        {
            // Arrange
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(
                    Rules.RazorGeneral.SchemaName,
                    new Dictionary<string, string>()
                    {
                        [Rules.RazorGeneral.RazorDefaultConfigurationProperty] = "13.37",
                        [Rules.RazorGeneral.RazorLangVersionProperty] = "1.0",
                    }),
                [Rules.RazorConfiguration.SchemaName] = TestProjectRuleSnapshot.CreateItems(
                    Rules.RazorConfiguration.SchemaName,
                    new Dictionary<string, Dictionary<string, string>>()
                    {
                        ["SomeExtension"] = new Dictionary<string, string>()
                        {
                            ["Extensions"] = "Razor-13.37"
                        }
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguration(projectState, out var configuration);

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
            var projectState = new Dictionary<string, IProjectRuleSnapshot>()
            {
                [Rules.RazorGeneral.SchemaName] = TestProjectRuleSnapshot.CreateProperties(
                    Rules.RazorGeneral.SchemaName,
                    new Dictionary<string, string>()
                    {
                        [Rules.RazorGeneral.RazorDefaultConfigurationProperty] = expectedConfigurationName,
                        [Rules.RazorGeneral.RazorLangVersionProperty] = "1.0",
                    }),
                [Rules.RazorConfiguration.SchemaName] = TestProjectRuleSnapshot.CreateItems(
                    Rules.RazorConfiguration.SchemaName,
                    new Dictionary<string, Dictionary<string, string>>()
                    {
                        ["UnconfiguredRazorConfiguration"] = new Dictionary<string, string>()
                        {
                            ["Extensions"] = "Razor-9.0"
                        },
                        [expectedConfigurationName] = new Dictionary<string, string>()
                        {
                            ["Extensions"] = expectedExtension1Name + ";" + expectedExtension2Name
                        }
                    }),
                [Rules.RazorExtension.PrimaryDataSourceItemType] = TestProjectRuleSnapshot.CreateItems(
                    Rules.RazorExtension.PrimaryDataSourceItemType,
                    new Dictionary<string, Dictionary<string, string>>()
                    {
                        [expectedExtension1Name] = new Dictionary<string, string>(),
                        [expectedExtension2Name] = new Dictionary<string, string>(),
                    })
            }.ToImmutableDictionary();

            // Act
            var result = DefaultRazorProjectHost.TryGetConfiguration(projectState, out var configuration);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedLanguageVersion, configuration.LanguageVersion);
            Assert.Equal(expectedConfigurationName, configuration.ConfigurationName);
            Assert.Collection(
                configuration.Extensions,
                extension => Assert.Equal(expectedExtension2Name, extension.ExtensionName),
                extension => Assert.Equal(expectedExtension1Name, extension.ExtensionName));
        }

        [ForegroundFact]
        public async Task DefaultRazorProjectHost_ForegroundThread_CreateAndDispose_Succeeds()
        {
            // Arrange
            var services = new TestProjectSystemServices("c:\\MyProject\\Test.csproj");
            var host = new DefaultRazorProjectHost(services, Workspace, ProjectManager);

            // Act & Assert
            await host.LoadAsync();
            Assert.Empty(ProjectManager.Projects);

            await host.DisposeAsync();
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task DefaultRazorProjectHost_BackgroundThread_CreateAndDispose_Succeeds()
        {
            // Arrange
            var services = new TestProjectSystemServices("c:\\MyProject\\Test.csproj");
            var host = new DefaultRazorProjectHost(services, Workspace, ProjectManager);

            // Act & Assert
            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact] // This can happen if the .xaml files aren't included correctly.
        public async Task DefaultRazorProjectHost_OnProjectChanged_NoRulesDefined()
        {
            // Arrange
            var changes = new TestProjectChangeDescription[]
            {
            };

            var services = new TestProjectSystemServices("Test.csproj");
            var host = new DefaultRazorProjectHost(services, Workspace, ProjectManager);

            // Act & Assert
            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_ReadsProperties_InitializesProject()
        {
            // Arrange
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorLangVersionProperty, "2.1");
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorDefaultConfigurationProperty, "MVC-2.1");

            ConfigurationItems.Item("MVC-2.1");
            ConfigurationItems.Property("MVC-2.1", Rules.RazorConfiguration.ExtensionsProperty, "MVC-2.1;Another-Thing");

            ExtensionItems.Item("MVC-2.1");
            ExtensionItems.Item("Another-Thing");

            DocumentItems.Item("File.cshtml");
            DocumentItems.Property("File.cshtml", Rules.RazorGenerateWithTargetPath.TargetPathProperty, "File.cshtml");

            var changes = new TestProjectChangeDescription[]
            {
                RazorGeneralProperties.ToChange(),
                ConfigurationItems.ToChange(),
                ExtensionItems.ToChange(),
                DocumentItems.ToChange(),
            };

            var services = new TestProjectSystemServices("c:\\MyProject\\Test.csproj");

            var host = new DefaultRazorProjectHost(services, Workspace, ProjectManager);

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert
            var snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("c:\\MyProject\\Test.csproj", snapshot.FilePath);

            Assert.Equal(RazorLanguageVersion.Version_2_1, snapshot.Configuration.LanguageVersion);
            Assert.Equal("MVC-2.1", snapshot.Configuration.ConfigurationName);
            Assert.Collection(
                snapshot.Configuration.Extensions,
                e => Assert.Equal("MVC-2.1", e.ExtensionName),
                e => Assert.Equal("Another-Thing", e.ExtensionName));

            Assert.Collection(
                snapshot.DocumentFilePaths.OrderBy(d => d),
                d =>
                {
                    var document = snapshot.GetDocument(d);
                    Assert.Equal("c:\\MyProject\\File.cshtml", document.FilePath);
                    Assert.Equal("File.cshtml", document.TargetPath);
                });

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_NoVersionFound_DoesNotIniatializeProject()
        {
            // Arrange
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorLangVersionProperty, "");
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorDefaultConfigurationProperty, "");

            ConfigurationItems.Item("TestConfiguration");

            ExtensionItems.Item("TestExtension");

            DocumentItems.Item("File.cshtml");

            var changes = new TestProjectChangeDescription[]
            {
                RazorGeneralProperties.ToChange(),
                ConfigurationItems.ToChange(),
                ExtensionItems.ToChange(),
                DocumentItems.ToChange(),
            };

            var services = new TestProjectSystemServices("c:\\MyProject\\Test.csproj");

            var host = new DefaultRazorProjectHost(services, Workspace, ProjectManager);

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert
            Assert.Empty(ProjectManager.Projects);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_UpdateProject_Succeeds()
        {
            // Arrange
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorLangVersionProperty, "2.1");
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorDefaultConfigurationProperty, "MVC-2.1");

            ConfigurationItems.Item("MVC-2.1");
            ConfigurationItems.Property("MVC-2.1", Rules.RazorConfiguration.ExtensionsProperty, "MVC-2.1;Another-Thing");

            ExtensionItems.Item("MVC-2.1");
            ExtensionItems.Item("Another-Thing");

            DocumentItems.Item("File.cshtml");
            DocumentItems.Property("File.cshtml", Rules.RazorGenerateWithTargetPath.TargetPathProperty, "File.cshtml");

            var changes = new TestProjectChangeDescription[]
            {
                RazorGeneralProperties.ToChange(),
                ConfigurationItems.ToChange(),
                ExtensionItems.ToChange(),
                DocumentItems.ToChange(),
            };

            var services = new TestProjectSystemServices("c:\\MyProject\\Test.csproj");

            var host = new DefaultRazorProjectHost(services, Workspace, ProjectManager);

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act - 1
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 1
            var snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("c:\\MyProject\\Test.csproj", snapshot.FilePath);

            Assert.Equal(RazorLanguageVersion.Version_2_1, snapshot.Configuration.LanguageVersion);
            Assert.Equal("MVC-2.1", snapshot.Configuration.ConfigurationName);
            Assert.Collection(
                snapshot.Configuration.Extensions,
                e => Assert.Equal("MVC-2.1", e.ExtensionName),
                e => Assert.Equal("Another-Thing", e.ExtensionName));

            Assert.Collection(
                snapshot.DocumentFilePaths.OrderBy(d => d),
                d => 
                {
                    var document = snapshot.GetDocument(d);
                    Assert.Equal("c:\\MyProject\\File.cshtml", document.FilePath);
                    Assert.Equal("File.cshtml", document.TargetPath);
                });

            // Act - 2
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorLangVersionProperty, "2.0");
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorDefaultConfigurationProperty, "MVC-2.0");
            ConfigurationItems.RemoveItem("MVC-2.1");
            ConfigurationItems.Item("MVC-2.0", new Dictionary<string, string>() { { "Extensions", "MVC-2.0;Another-Thing" }, });
            ExtensionItems.Item("MVC-2.0");
            DocumentItems.Item("c:\\AnotherProject\\AnotherFile.cshtml", new Dictionary<string, string>()
            {
                { Rules.RazorGenerateWithTargetPath.TargetPathProperty, "Pages\\AnotherFile.cshtml" },
            });

            changes = new TestProjectChangeDescription[]
            {
                RazorGeneralProperties.ToChange(changes[0].After),
                ConfigurationItems.ToChange(changes[1].After),
                ExtensionItems.ToChange(changes[2].After),
                DocumentItems.ToChange(changes[3].After),
            };

            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 2
            snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("c:\\MyProject\\Test.csproj", snapshot.FilePath);

            Assert.Equal(RazorLanguageVersion.Version_2_0, snapshot.Configuration.LanguageVersion);
            Assert.Equal("MVC-2.0", snapshot.Configuration.ConfigurationName);
            Assert.Collection(
                snapshot.Configuration.Extensions,
                e => Assert.Equal("Another-Thing", e.ExtensionName),
                e => Assert.Equal("MVC-2.0", e.ExtensionName));

            Assert.Collection(
                snapshot.DocumentFilePaths.OrderBy(d => d),
                d =>
                {
                    var document = snapshot.GetDocument(d);
                    Assert.Equal("c:\\AnotherProject\\AnotherFile.cshtml", document.FilePath);
                    Assert.Equal("Pages\\AnotherFile.cshtml", document.TargetPath);
                },
                d =>
                {
                    var document = snapshot.GetDocument(d);
                    Assert.Equal("c:\\MyProject\\File.cshtml", document.FilePath);
                    Assert.Equal("File.cshtml", document.TargetPath);
                });

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_VersionRemoved_DeinitializesProject()
        {
            // Arrange
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorLangVersionProperty, "2.1");
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorDefaultConfigurationProperty, "MVC-2.1");

            ConfigurationItems.Item("MVC-2.1");
            ConfigurationItems.Property("MVC-2.1", Rules.RazorConfiguration.ExtensionsProperty, "MVC-2.1;Another-Thing");

            ExtensionItems.Item("MVC-2.1");
            ExtensionItems.Item("Another-Thing");

            DocumentItems.Item("File.cshtml");
            DocumentItems.Property("File.cshtml", Rules.RazorGenerateWithTargetPath.TargetPathProperty, "File.cshtml");

            var changes = new TestProjectChangeDescription[]
            {
                RazorGeneralProperties.ToChange(),
                ConfigurationItems.ToChange(),
                ExtensionItems.ToChange(),
                DocumentItems.ToChange(),
            };

            var services = new TestProjectSystemServices("c:\\MyProject\\Test.csproj");

            var host = new DefaultRazorProjectHost(services, Workspace, ProjectManager);

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act - 1
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 1
            var snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("c:\\MyProject\\Test.csproj", snapshot.FilePath);

            Assert.Equal(RazorLanguageVersion.Version_2_1, snapshot.Configuration.LanguageVersion);
            Assert.Equal("MVC-2.1", snapshot.Configuration.ConfigurationName);
            Assert.Collection(
                snapshot.Configuration.Extensions,
                e => Assert.Equal("MVC-2.1", e.ExtensionName),
                e => Assert.Equal("Another-Thing", e.ExtensionName));

            // Act - 2
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorLangVersionProperty, "");
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorDefaultConfigurationProperty, "");

            changes = new TestProjectChangeDescription[]
            {
                RazorGeneralProperties.ToChange(changes[0].After),
                ConfigurationItems.ToChange(changes[1].After),
                ExtensionItems.ToChange(changes[2].After),
                DocumentItems.ToChange(changes[3].After),
            };

            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 2
            Assert.Empty(ProjectManager.Projects);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_AfterDispose_IgnoresUpdate()
        {
            // Arrange
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorLangVersionProperty, "2.1");
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorDefaultConfigurationProperty, "MVC-2.1");

            ConfigurationItems.Item("MVC-2.1");
            ConfigurationItems.Property("MVC-2.1", Rules.RazorConfiguration.ExtensionsProperty, "MVC-2.1;Another-Thing");

            ExtensionItems.Item("MVC-2.1");
            ExtensionItems.Item("Another-Thing");

            DocumentItems.Item("File.cshtml");
            DocumentItems.Property("File.cshtml", Rules.RazorGenerateWithTargetPath.TargetPathProperty, "File.cshtml");

            var changes = new TestProjectChangeDescription[]
            {
                RazorGeneralProperties.ToChange(),
                ConfigurationItems.ToChange(),
                ExtensionItems.ToChange(),
                DocumentItems.ToChange(),
            };

            var services = new TestProjectSystemServices("c:\\MyProject\\Test.csproj");

            var host = new DefaultRazorProjectHost(services, Workspace, ProjectManager);

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act - 1
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 1
            var snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("c:\\MyProject\\Test.csproj", snapshot.FilePath);

            Assert.Equal(RazorLanguageVersion.Version_2_1, snapshot.Configuration.LanguageVersion);
            Assert.Equal("MVC-2.1", snapshot.Configuration.ConfigurationName);
            Assert.Collection(
                snapshot.Configuration.Extensions,
                e => Assert.Equal("MVC-2.1", e.ExtensionName),
                e => Assert.Equal("Another-Thing", e.ExtensionName));

            // Act - 2
            await Task.Run(async () => await host.DisposeAsync());

            // Assert - 2
            Assert.Empty(ProjectManager.Projects);

            // Act - 3
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorLangVersionProperty, "2.0");
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorDefaultConfigurationProperty, "MVC-2.0");
            ConfigurationItems.Item("MVC-2.0", new Dictionary<string, string>() { { "Extensions", "MVC-2.0;Another-Thing" }, });

            changes = new TestProjectChangeDescription[]
            {
                RazorGeneralProperties.ToChange(changes[0].After),
                ConfigurationItems.ToChange(changes[1].After),
                ExtensionItems.ToChange(changes[2].After),
                DocumentItems.ToChange(changes[3].After),
            };

            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 3
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectRenamed_RemovesHostProject_CopiesConfiguration()
        {
            // Arrange
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorLangVersionProperty, "2.1");
            RazorGeneralProperties.Property(Rules.RazorGeneral.RazorDefaultConfigurationProperty, "MVC-2.1");

            ConfigurationItems.Item("MVC-2.1");
            ConfigurationItems.Property("MVC-2.1", Rules.RazorConfiguration.ExtensionsProperty, "MVC-2.1;Another-Thing");

            ExtensionItems.Item("MVC-2.1");
            ExtensionItems.Item("Another-Thing");

            DocumentItems.Item("File.cshtml");
            DocumentItems.Property("File.cshtml", Rules.RazorGenerateWithTargetPath.TargetPathProperty, "File.cshtml");

            var changes = new TestProjectChangeDescription[]
            {
                RazorGeneralProperties.ToChange(),
                ConfigurationItems.ToChange(),
                ExtensionItems.ToChange(),
                DocumentItems.ToChange(),
            };

            var services = new TestProjectSystemServices("c:\\MyProject\\Test.csproj");

            var host = new DefaultRazorProjectHost(services, Workspace, ProjectManager);

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act - 1
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 1
            var snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("c:\\MyProject\\Test.csproj", snapshot.FilePath);
            Assert.Same("MVC-2.1", snapshot.Configuration.ConfigurationName);

            // Act - 2
            services.UnconfiguredProject.FullPath = "c:\\AnotherProject\\Test2.csproj";
            await Task.Run(async () => await host.OnProjectRenamingAsync());

            // Assert - 1
            snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("c:\\AnotherProject\\Test2.csproj", snapshot.FilePath);
            Assert.Same("MVC-2.1", snapshot.Configuration.ConfigurationName);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        private class TestProjectSnapshotManager : DefaultProjectSnapshotManager
        {
            public TestProjectSnapshotManager(ForegroundDispatcher dispatcher, Workspace workspace) 
                : base(dispatcher, Mock.Of<ErrorReporter>(), Array.Empty<ProjectSnapshotChangeTrigger>(), workspace)
            {
            }
        }
    }
}
