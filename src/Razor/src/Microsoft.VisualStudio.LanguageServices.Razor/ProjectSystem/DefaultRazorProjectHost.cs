// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.TextManager.Interop;
using Item = System.Collections.Generic.KeyValuePair<string, System.Collections.Immutable.IImmutableDictionary<string, string>>;

#if WORKSPACE_PROJECT_CONTEXT_FACTORY
using IWorkspaceProjectContextFactory = Microsoft.VisualStudio.LanguageServices.ProjectSystem.IWorkspaceProjectContextFactory2;
#endif

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // Somewhat similar to https://github.com/dotnet/project-system/blob/fa074d228dcff6dae9e48ce43dd4a3a5aa22e8f0/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/LanguageServices/LanguageServiceHost.cs
    //
    // This class is responsible for intializing the Razor ProjectSnapshotManager for cases where
    // MSBuild provides configuration support (>= 2.1).
    [AppliesTo("DotNetCoreRazor & DotNetCoreRazorConfiguration")]
    [ExportVsProfferedProjectService(typeof(IVsContainedLanguageProjectNameProvider))]
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    internal class DefaultRazorProjectHost : RazorProjectHostBase, IVsContainedLanguageProjectNameProvider
    {
        private IDisposable _subscription;

        [ImportingConstructor]
        public DefaultRazorProjectHost(
            IUnconfiguredProjectCommonServices commonServices,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace,
            Lazy<IWorkspaceProjectContextFactory> projectContextFactory)
            : base(commonServices, workspace, projectContextFactory)
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
                ruleNames: new string[]
                {
                    Rules.RazorGeneral.SchemaName,
                    Rules.RazorConfiguration.SchemaName,
                    Rules.RazorExtension.SchemaName,
                    Rules.RazorGenerateWithTargetPath.SchemaName,
                    ManagedProjectSystemSchema.CompilerCommandLineArgs.SchemaName,
                    ManagedProjectSystemSchema.ConfigurationGeneral.SchemaName,
                    ManagedProjectSystemSchema.ResolvedCompilationReference.SchemaName,
                });
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

                        // We need to deal with the case where the project was uninitialized, but now
                        // is valid for Razor. In that case we might have previously seen all of the documents
                        // but ignored them because the project wasn't active.
                        //
                        // So what we do to deal with this, is that we 'remove' all changed and removed items
                        // and then we 'add' all current items. This allows minimal churn to the PSM, but still
                        // makes us up to date.
                        var documents = GetCurrentDocuments(update.Value);
                        var changedDocuments = GetChangedAndRemovedDocuments(update.Value);

                        var references = GetReferences(update.Value);
                        TryGetCommandLineOptions(update.Value.CurrentState, out var commandLineOptions);

                        await UpdateAsync(() =>
                        {
                            UpdateProjectUnsafe(hostProject);
                            UpdateWorkspaceProjectOptionsUnsafe(commandLineOptions);
                            UpdateWorkspaceProjectReferencesUnsafe(references);

                            for (var i = 0; i < changedDocuments.Length; i++)
                            {
                                RemoveDocumentUnsafe(changedDocuments[i]);
                            }

                            for (var i = 0; i < documents.Length; i++)
                            {
                                AddDocumentUnsafe(documents[i]);
                            }
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        // Ok we can't find a configuration. Let's assume this project isn't using Razor then.
                        await UpdateAsync(UninitializeProjectUnsafe).ConfigureAwait(false);
                    }
                });
            }, registerFaultHandler: true);
        }

        // Internal for testing
        internal static bool TryGetConfiguration(
            IImmutableDictionary<string, IProjectRuleSnapshot> state,
            out RazorConfiguration configuration)
        {
            if (!TryGetDefaultConfiguration(state, out var defaultConfiguration))
            {
                configuration = null;
                return false;
            }

            if (!TryGetLanguageVersion(state, out var languageVersion))
            {
                configuration = null;
                return false;
            }

            if (!TryGetConfigurationItem(defaultConfiguration, state, out var configurationItem))
            {
                configuration = null;
                return false;
            }

            if (!TryGetExtensionNames(configurationItem, out var extensionNames))
            {
                configuration = null;
                return false;
            }

            if (!TryGetExtensions(extensionNames, state, out var extensions))
            {
                configuration = null;
                return false;
            }

            configuration = new ProjectSystemRazorConfiguration(languageVersion, configurationItem.Key, extensions);
            return true;
        }


        // Internal for testing
        internal static bool TryGetDefaultConfiguration(
            IImmutableDictionary<string, IProjectRuleSnapshot> state,
            out string defaultConfiguration)
        {
            if (!state.TryGetValue(Rules.RazorGeneral.SchemaName, out var rule))
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
        internal static bool TryGetLanguageVersion(
            IImmutableDictionary<string, IProjectRuleSnapshot> state,
            out RazorLanguageVersion languageVersion)
        {
            if (!state.TryGetValue(Rules.RazorGeneral.SchemaName, out var rule))
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
            IImmutableDictionary<string, IProjectRuleSnapshot> state,
            out Item configurationItem)
        {
            if (!state.TryGetValue(Rules.RazorConfiguration.PrimaryDataSourceItemType, out var configurationState))
            {
                configurationItem = default(Item);
                return false;
            }

            var items = configurationState.Items;
            foreach (var item in items)
            {
                if (item.Key == configuration)
                {
                    configurationItem = item;
                    return true;
                }
            }

            configurationItem = default(Item);
            return false;
        }

        // Internal for testing
        internal static bool TryGetExtensionNames(
            Item configurationItem, 
            out string[] configuredExtensionNames)
        {
            if (!configurationItem.Value.TryGetValue(Rules.RazorConfiguration.ExtensionsProperty, out var extensionNames))
            {
                configuredExtensionNames = null;
                return false;
            }

            if (string.IsNullOrEmpty(extensionNames))
            {
                configuredExtensionNames = null;
                return false;
            }

            configuredExtensionNames = extensionNames.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return true;
        }

        // Internal for testing
        internal static bool TryGetExtensions(
            string[] extensionNames,
            IImmutableDictionary<string, IProjectRuleSnapshot> state, 
            out ProjectSystemRazorExtension[] extensions)
        {
            if (!state.TryGetValue(Rules.RazorExtension.PrimaryDataSourceItemType, out var rule))
            {
                extensions = null;
                return false;
            }

            var items = rule.Items;
            var extensionList = new List<ProjectSystemRazorExtension>();
            foreach (var item in items)
            {
                var extensionName = item.Key;
                if (extensionNames.Contains(extensionName))
                {
                    extensionList.Add(new ProjectSystemRazorExtension(extensionName));
                }
            }

            extensions = extensionList.ToArray();
            return true;
        }


        // This is temporary code for initializing the companion project. We expect
        // this to be provided by the Managed Project System in the near future.
        internal static bool TryGetReferences(
            IImmutableDictionary<string, IProjectRuleSnapshot> state,
            out string[] references)
        {
            if (!state.TryGetValue(ManagedProjectSystemSchema.ResolvedCompilationReference.ItemName, out var rule))
            {
                references = null;
                return false;
            }

            var items = rule.Items;
            var referencesList = new List<string>();
            foreach (var item in items)
            {
                var reference = item.Key;
                if (!referencesList.Contains(reference, FilePathComparer.Instance))
                {
                    referencesList.Add(reference);
                }
            }

            references = referencesList.ToArray();
            return true;
        }

        // This is temporary code for initializing the companion project. We expect
        // this to be provided by the Managed Project System in the near future.
        internal static bool TryGetCommandLineOptions(
            IImmutableDictionary<string, IProjectRuleSnapshot> state,
            out string commandLineOptions)
        {
            if (!state.TryGetValue(ManagedProjectSystemSchema.CompilerCommandLineArgs.ItemName, out var rule))
            {
                commandLineOptions = null;
                return false;
            }

            commandLineOptions = string.Join(" ", rule.Items.Select(kvp => kvp.Key));
            return true;
        }

        // This is temporary code for initializing the companion project. We expect
        // this to be provided by the Managed Project System in the near future.
        internal static bool TryGetTargetPath(
            IImmutableDictionary<string, IProjectRuleSnapshot> state,
            out string targetPath)
        {
            if (!state.TryGetValue(ManagedProjectSystemSchema.ConfigurationGeneral.SchemaName, out var rule))
            {
                targetPath = null;
                return false;
            }

            if (!rule.Properties.TryGetValue(ManagedProjectSystemSchema.ConfigurationGeneral.TargetPathPropertyName, out targetPath))
            {
                targetPath = null;
                return false;
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = null;
                return false;
            }

            return true;
        }
        
        private HostDocument[] GetCurrentDocuments(IProjectSubscriptionUpdate update)
        {
            if (!update.CurrentState.TryGetValue(Rules.RazorGenerateWithTargetPath.SchemaName, out var rule))
            {
                return Array.Empty<HostDocument>();
            }

            var documents = new List<HostDocument>();
            foreach (var kvp in rule.Items)
            {
                if (kvp.Value.TryGetValue(Rules.RazorGenerateWithTargetPath.TargetPathProperty, out var targetPath) &&
                    !string.IsNullOrWhiteSpace(kvp.Key) &&
                    !string.IsNullOrWhiteSpace(targetPath))
                {
                    var filePath = CommonServices.UnconfiguredProject.MakeRooted(kvp.Key);
                    documents.Add(new HostDocument(filePath, targetPath));
                }
            }

            return documents.ToArray();
        }

        private HostDocument[] GetChangedAndRemovedDocuments(IProjectSubscriptionUpdate update)
        {
            if (!update.ProjectChanges.TryGetValue(Rules.RazorGenerateWithTargetPath.SchemaName, out var rule))
            {
                return Array.Empty<HostDocument>();
            }

            var documents = new List<HostDocument>();
            foreach (var key in rule.Difference.RemovedItems.Concat(rule.Difference.ChangedItems))
            {
                if (rule.Before.Items.TryGetValue(key, out var value))
                {
                    if (value.TryGetValue(Rules.RazorGenerateWithTargetPath.TargetPathProperty, out var targetPath) &&
                        !string.IsNullOrWhiteSpace(key) &&
                        !string.IsNullOrWhiteSpace(targetPath))
                    {
                        var filePath = CommonServices.UnconfiguredProject.MakeRooted(key);
                        documents.Add(new HostDocument(filePath, targetPath));
                    }
                }
            }

            return documents.ToArray();
        }

        // This is temporary code for initializing the companion project. We expect
        // this to be provided by the Managed Project System in the near future.
        private string[] GetReferences(IProjectSubscriptionUpdate update)
        {
            if (!TryGetReferences(update.CurrentState, out var references))
            {
                return Array.Empty<string>();
            }

            if (TryGetTargetPath(update.CurrentState, out var targetPath))
            {
                references = references.Concat(new[] { targetPath, }).ToArray();
            }

            return references;
        }

        // This is temporary code for initializing the companion project. We expect
        // this to be provided by the Managed Project System in the near future.
        public int GetProjectName([In] uint itemid, [MarshalAs(UnmanagedType.BStr)] out string pbstrProjectName)
        {
            if (Current == null)
            {
                pbstrProjectName = null;

                return VSConstants.E_INVALIDARG;
            }

            pbstrProjectName = Path.GetFileNameWithoutExtension(Current.FilePath) + " (Razor)";
            return VSConstants.S_OK;
        }
    }
}