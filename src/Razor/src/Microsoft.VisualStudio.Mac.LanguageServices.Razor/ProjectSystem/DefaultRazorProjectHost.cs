// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.ProjectSystem
{
    internal class DefaultRazorProjectHost : RazorProjectHostBase
    {
        private const string RazorLangVersionProperty = "RazorLangVersion";
        private const string RazorDefaultConfigurationProperty = "RazorDefaultConfiguration";
        private const string RazorExtensionItemType = "RazorExtension";
        private const string RazorConfigurationItemType = "RazorConfiguration";
        private const string RazorConfigurationItemTypeExtensionsProperty = "Extensions";

        public DefaultRazorProjectHost(
            DotNetProject project,
            ForegroundDispatcher foregroundDispatcher,
            ProjectSnapshotManagerBase projectSnapshotManager)
            : base(project, foregroundDispatcher, projectSnapshotManager)
        {
        }

        protected override async Task OnProjectChangedAsync()
        {
            ForegroundDispatcher.AssertBackgroundThread();

            await ExecuteWithLockAsync(async () =>
            {
                var projectProperties = DotNetProject.MSBuildProject.EvaluatedProperties;
                var projectItems = DotNetProject.MSBuildProject.EvaluatedItems;

                if (TryGetConfiguration(projectProperties, projectItems, out var configuration))
                {
                    var hostProject = new HostProject(DotNetProject.FileName.FullPath, configuration);
                    await UpdateHostProjectUnsafeAsync(hostProject).ConfigureAwait(false);
                }
                else
                {
                    // Ok we can't find a configuration. Let's assume this project isn't using Razor then.
                    await UpdateHostProjectUnsafeAsync(null).ConfigureAwait(false);
                }
            });
        }

        // Internal for testing
        internal static bool TryGetConfiguration(
            IMSBuildEvaluatedPropertyCollection projectProperties,
            IEnumerable<IMSBuildItemEvaluated> projectItems,
            out RazorConfiguration configuration)
        {
            if (!TryGetDefaultConfiguration(projectProperties, out var defaultConfiguration))
            {
                configuration = null;
                return false;
            }

            if (!TryGetLanguageVersion(projectProperties, out var languageVersion))
            {
                configuration = null;
                return false;
            }

            if (!TryGetConfigurationItem(defaultConfiguration, projectItems, out var configurationItem))
            {
                configuration = null;
                return false;
            }

            if (!TryGetConfiguredExtensionNames(configurationItem, out var configuredExtensionNames))
            {
                configuration = null;
                return false;
            }

            var extensions = GetExtensions(configuredExtensionNames, projectItems);
            configuration = new ProjectSystemRazorConfiguration(languageVersion, configurationItem.Include, extensions);
            return true;
        }


        // Internal for testing
        internal static bool TryGetDefaultConfiguration(IMSBuildEvaluatedPropertyCollection projectProperties, out string defaultConfiguration)
        {
            defaultConfiguration = projectProperties.GetValue(RazorDefaultConfigurationProperty);
            if (string.IsNullOrEmpty(defaultConfiguration))
            {
                defaultConfiguration = null;
                return false;
            }

            return true;
        }

        // Internal for testing
        internal static bool TryGetLanguageVersion(IMSBuildEvaluatedPropertyCollection projectProperties, out RazorLanguageVersion languageVersion)
        {
            var languageVersionValue = projectProperties.GetValue(RazorLangVersionProperty);
            if (string.IsNullOrEmpty(languageVersionValue))
            {
                languageVersion = null;
                return false;
            }

            if (!RazorLanguageVersion.TryParse(languageVersionValue, out languageVersion))
            {
                languageVersion = RazorLanguageVersion.Latest;
            }

            return true;
        }

        // Internal for testing
        internal static bool TryGetConfigurationItem(
            string configuration,
            IEnumerable<IMSBuildItemEvaluated> projectItems,
            out IMSBuildItemEvaluated configurationItem)
        {
            foreach (var item in projectItems)
            {
                if (item.Name == RazorConfigurationItemType && item.Include == configuration)
                {
                    configurationItem = item;
                    return true;
                }
            }

            configurationItem = null;
            return false;
        }

        // Internal for testing
        internal static bool TryGetConfiguredExtensionNames(IMSBuildItemEvaluated configurationItem, out string[] configuredExtensionNames)
        {
            var extensionNamesValue = configurationItem.Metadata.GetValue(RazorConfigurationItemTypeExtensionsProperty);

            if (string.IsNullOrEmpty(extensionNamesValue))
            {
                configuredExtensionNames = null;
                return false;
            }
            
            configuredExtensionNames = extensionNamesValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return true;
        }

        // Internal for testing
        internal static ProjectSystemRazorExtension[] GetExtensions(
            string[] configuredExtensionNames,
            IEnumerable<IMSBuildItemEvaluated> projectItems)
        {
            var extensions = new List<ProjectSystemRazorExtension>();

            foreach (var item in projectItems)
            {
                if (item.Name != RazorExtensionItemType)
                {
                    // Not a RazorExtension
                    continue;
                }

                var extensionName = item.Include;
                if (configuredExtensionNames.Contains(extensionName))
                {
                    extensions.Add(new ProjectSystemRazorExtension(extensionName));
                }
            }

            return extensions.ToArray();
        }
    }
}
