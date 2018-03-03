// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem;

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
                    var languageVersion = update.Value.CurrentState[Rules.RazorGeneral.SchemaName].Properties[Rules.RazorGeneral.RazorLangVersionProperty];
                    var defaultConfiguration = update.Value.CurrentState[Rules.RazorGeneral.SchemaName].Properties[Rules.RazorGeneral.RazorDefaultConfigurationProperty];

                    RazorConfiguration configuration = null;
                    if (!string.IsNullOrEmpty(languageVersion) && !string.IsNullOrEmpty(defaultConfiguration))
                    {
                        if (!RazorLanguageVersion.TryParse(languageVersion, out var parsedVersion))
                        {
                            parsedVersion = RazorLanguageVersion.Latest;
                        }

                        var extensions = update.Value.CurrentState[Rules.RazorExtension.PrimaryDataSourceItemType].Items.Select(e =>
                        {
                            return new ProjectSystemRazorExtension(e.Key);
                        }).ToArray();

                        var configurations = update.Value.CurrentState[Rules.RazorConfiguration.PrimaryDataSourceItemType].Items.Select(c =>
                        {
                            var includedExtensions = c.Value[Rules.RazorConfiguration.ExtensionsProperty]
                                .Split(';')
                                .Select(name => extensions.Where(e => e.ExtensionName == name).FirstOrDefault())
                                .Where(e => e != null)
                                .ToArray();

                            return new ProjectSystemRazorConfiguration(parsedVersion, c.Key, includedExtensions);
                        }).ToArray();

                        configuration = configurations.Where(c => c.ConfigurationName == defaultConfiguration).FirstOrDefault();
                    }

                    if (configuration == null)
                    {
                        // Ok we can't find a language version. Let's assume this project isn't using Razor then.
                        await UpdateProjectUnsafeAsync(null).ConfigureAwait(false);
                        return;
                    }

                    var hostProject = new HostProject(CommonServices.UnconfiguredProject.FullPath, configuration);
                    await UpdateProjectUnsafeAsync(hostProject).ConfigureAwait(false);
                });
            }, registerFaultHandler: true);
        }
    }
}