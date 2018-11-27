// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using ProjectState = System.Collections.Immutable.IImmutableDictionary<string, Microsoft.VisualStudio.ProjectSystem.Properties.IProjectRuleSnapshot>;
using ProjectStateItem = System.Collections.Generic.KeyValuePair<string, System.Collections.Immutable.IImmutableDictionary<string, string>>;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // Somewhat similar to https://github.com/dotnet/project-system/blob/fa074d228dcff6dae9e48ce43dd4a3a5aa22e8f0/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/LanguageServices/LanguageServiceHost.cs
    //
    // This class is responsible for intializing the Razor ProjectSnapshotManager for cases where
    // MSBuild provides configuration support (>= 2.1).
    [AppliesTo("DotNetCoreRazor & DotNetCoreRazorConfiguration")]
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    internal class DefaultRazorProjectHost : RazorProjectHostBase
    {
        private IDisposable _subscription;

        [ImportingConstructor]
        public DefaultRazorProjectHost(
            IUnconfiguredProjectCommonServices commonServices,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
            : base(commonServices, workspace)
        {
        }

        // Internal for testing
        internal DefaultRazorProjectHost(
            IUnconfiguredProjectCommonServices commonServices,
             Workspace workspace,
             ProjectSnapshotManagerBase projectManager)
            : base(commonServices, workspace, projectManager)
        {
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await base.InitializeCoreAsync(cancellationToken).ConfigureAwait(false);

            // Don't try to evaluate any properties here since the project is still loading and we require access
            // to the UI thread to push our updates.
            //
            // Just subscribe and handle the notification later.
            // Don't try to evaluate any properties here since the project is still loading and we require access
            // to the UI thread to push our updates.
            //
            // Just subscribe and handle the notification later.
            var receiver = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(OnProjectChanged);
            _subscription = CommonServices.ActiveConfiguredProjectSubscription.JointRuleSource.SourceBlock.LinkTo(
                receiver,
                initialDataAsNew: true,
                suppressVersionOnlyUpdates: true,
                ruleNames: new string[] { Rules.RazorGeneral.SchemaName, Rules.RazorConfiguration.SchemaName, Rules.RazorExtension.SchemaName });
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            await base.DisposeCoreAsync(initialized).ConfigureAwait(false);

            if (initialized)
            {
                _subscription.Dispose();
            }
        }

        // Internal for testing
        internal async Task OnProjectChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> update)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            await CommonServices.TasksService.LoadedProjectAsync(async () =>
            {
                await ExecuteWithLock(async () =>
                {
                    if (TryGetConfiguration(update.Value.CurrentState, out var configuration))
                    {
                        var hostProject = new HostProject(CommonServices.UnconfiguredProject.FullPath, configuration);
                        await UpdateProjectUnsafeAsync(hostProject).ConfigureAwait(false);
                    }
                    else
                    {
                        // Ok we can't find a configuration. Let's assume this project isn't using Razor then.
                        await UpdateProjectUnsafeAsync(null).ConfigureAwait(false);
                    }
                });
            }, registerFaultHandler: true);
        }

        // Internal for testing
        internal static bool TryGetConfiguration(
            ProjectState projectState,
            out RazorConfiguration configuration)
        {
            if (!TryGetDefaultConfiguration(projectState, out var defaultConfiguration))
            {
                configuration = null;
                return false;
            }

            if (!TryGetLanguageVersion(projectState, out var languageVersion))
            {
                configuration = null;
                return false;
            }

            if (!TryGetConfigurationItem(defaultConfiguration, projectState, out var configurationItem))
            {
                configuration = null;
                return false;
            }

            if (!TryGetConfiguredExtensionNames(configurationItem, out var configuredExtensionNames))
            {
                configuration = null;
                return false;
            }

            if (!TryGetExtensions(configuredExtensionNames, projectState, out var extensions))
            {
                configuration = null;
                return false;
            }

            configuration = new ProjectSystemRazorConfiguration(languageVersion, configurationItem.Key, extensions);
            return true;
        }


        // Internal for testing
        internal static bool TryGetDefaultConfiguration(ProjectState projectState, out string defaultConfiguration)
        {
            if (!projectState.TryGetValue(Rules.RazorGeneral.SchemaName, out var rule))
            {
                defaultConfiguration = null;
                return false;
            }

            if (!rule.Properties.TryGetValue(Rules.RazorGeneral.RazorDefaultConfigurationProperty, out defaultConfiguration))
            {
                defaultConfiguration = null;
                return false;
            }

            if (string.IsNullOrEmpty(defaultConfiguration))
            {
                defaultConfiguration = null;
                return false;
            }

            return true;
        }

        // Internal for testing
        internal static bool TryGetLanguageVersion(ProjectState projectState, out RazorLanguageVersion languageVersion)
        {
            if (!projectState.TryGetValue(Rules.RazorGeneral.SchemaName, out var rule))
            {
                languageVersion = null;
                return false;
            }

            if (!rule.Properties.TryGetValue(Rules.RazorGeneral.RazorLangVersionProperty, out var languageVersionValue))
            {
                languageVersion = null;
                return false;
            }

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
            ProjectState projectState,
            out ProjectStateItem configurationItem)
        {
            if (!projectState.TryGetValue(Rules.RazorConfiguration.PrimaryDataSourceItemType, out var configurationState))
            {
                configurationItem = default(ProjectStateItem);
                return false;
            }

            var razorConfigurationItems = configurationState.Items;
            foreach (var item in razorConfigurationItems)
            {
                if (item.Key == configuration)
                {
                    configurationItem = item;
                    return true;
                }
            }

            configurationItem = default(ProjectStateItem);
            return false;
        }

        // Internal for testing
        internal static bool TryGetConfiguredExtensionNames(ProjectStateItem configurationItem, out string[] configuredExtensionNames)
        {
            if (!configurationItem.Value.TryGetValue(Rules.RazorConfiguration.ExtensionsProperty, out var extensionNamesValue))
            {
                configuredExtensionNames = null;
                return false;
            }

            if (string.IsNullOrEmpty(extensionNamesValue))
            {
                configuredExtensionNames = null;
                return false;
            }

            configuredExtensionNames = extensionNamesValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return true;
        }

        // Internal for testing
        internal static bool TryGetExtensions(string[] configuredExtensionNames, ProjectState projectState, out ProjectSystemRazorExtension[] extensions)
        {
            if (!projectState.TryGetValue(Rules.RazorExtension.PrimaryDataSourceItemType, out var extensionState))
            {
                extensions = null;
                return false;
            }

            var extensionItems = extensionState.Items;
            var extensionList = new List<ProjectSystemRazorExtension>();
            foreach (var item in extensionItems)
            {
                var extensionName = item.Key;
                if (configuredExtensionNames.Contains(extensionName))
                {
                    extensionList.Add(new ProjectSystemRazorExtension(extensionName));
                }
            }

            extensions = extensionList.ToArray();
            return true;
        }
    }
}