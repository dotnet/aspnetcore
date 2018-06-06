// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem;
using ContentItem = Microsoft.CodeAnalysis.Razor.ProjectSystem.ManagedProjectSystemSchema.ContentItem;
using ItemReference = Microsoft.CodeAnalysis.Razor.ProjectSystem.ManagedProjectSystemSchema.ItemReference;
using NoneItem = Microsoft.CodeAnalysis.Razor.ProjectSystem.ManagedProjectSystemSchema.NoneItem;
using ResolvedCompilationReference = Microsoft.CodeAnalysis.Razor.ProjectSystem.ManagedProjectSystemSchema.ResolvedCompilationReference;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // Somewhat similar to https://github.com/dotnet/project-system/blob/fa074d228dcff6dae9e48ce43dd4a3a5aa22e8f0/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/LanguageServices/LanguageServiceHost.cs
    //
    // This class is responsible for intializing the Razor ProjectSnapshotManager for cases where
    // MSBuild does not provides configuration support (SDK < 2.1).
    [AppliesTo("(DotNetCoreRazor | DotNetCoreWeb) & !DotNetCoreRazorConfiguration")]
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    internal class FallbackRazorProjectHost : RazorProjectHostBase
    {
        private const string MvcAssemblyName = "Microsoft.AspNetCore.Mvc.Razor";
        private const string MvcAssemblyFileName = "Microsoft.AspNetCore.Mvc.Razor.dll";

        private IDisposable _subscription;

        [ImportingConstructor]
        public FallbackRazorProjectHost(
            IUnconfiguredProjectCommonServices commonServices,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
            : base(commonServices, workspace)
        {
        }

        // Internal for testing
        internal FallbackRazorProjectHost(
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
            var receiver = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(OnProjectChanged);
            _subscription = CommonServices.ActiveConfiguredProjectSubscription.JointRuleSource.SourceBlock.LinkTo(
                receiver,
                initialDataAsNew: true,
                suppressVersionOnlyUpdates: true,
                ruleNames: new string[]
                {
                    ResolvedCompilationReference.SchemaName,
                    ContentItem.SchemaName,
                    NoneItem.SchemaName,
                },
                linkOptions: new DataflowLinkOptions() { PropagateCompletion = true });
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
                    string mvcReferenceFullPath = null;
                    if (update.Value.CurrentState.ContainsKey(ResolvedCompilationReference.SchemaName))
                    {
                        var references = update.Value.CurrentState[ResolvedCompilationReference.SchemaName].Items;
                        foreach (var reference in references)
                        {
                            if (reference.Key.EndsWith(MvcAssemblyFileName, StringComparison.OrdinalIgnoreCase))
                            {
                                mvcReferenceFullPath = reference.Key;
                                break;
                            }
                        }
                    }

                    if (mvcReferenceFullPath == null)
                    {
                        // Ok we can't find an MVC version. Let's assume this project isn't using Razor then.
                        await UpdateAsync(UninitializeProjectUnsafe).ConfigureAwait(false);
                        return;
                    }

                    var version = GetAssemblyVersion(mvcReferenceFullPath);
                    if (version == null)
                    {
                        // Ok we can't find an MVC version. Let's assume this project isn't using Razor then.
                        await UpdateAsync(UninitializeProjectUnsafe).ConfigureAwait(false);
                        return;
                    }

                    var configuration = FallbackRazorConfiguration.SelectConfiguration(version);
                    var hostProject = new HostProject(CommonServices.UnconfiguredProject.FullPath, configuration);

                    // We need to deal with the case where the project was uninitialized, but now
                    // is valid for Razor. In that case we might have previously seen all of the documents
                    // but ignored them because the project wasn't active.
                    //
                    // So what we do to deal with this, is that we 'remove' all changed and removed items
                    // and then we 'add' all current items. This allows minimal churn to the PSM, but still
                    // makes us up-to-date.
                    var documents = GetCurrentDocuments(update.Value);
                    var changedDocuments = GetChangedAndRemovedDocuments(update.Value);

                    await UpdateAsync(() =>
                    {
                        UpdateProjectUnsafe(hostProject);

                        for (var i = 0; i < changedDocuments.Length; i++)
                        {
                            RemoveDocumentUnsafe(changedDocuments[i]);
                        }

                        for (var i = 0; i < documents.Length; i++)
                        {
                            AddDocumentUnsafe(documents[i]);
                        }
                    }).ConfigureAwait(false);
                });
            }, registerFaultHandler: true);
        }

        // virtual for overriding in tests
        protected virtual Version GetAssemblyVersion(string filePath)
        {
            return ReadAssemblyVersion(filePath);
        }

        // Internal for testing
        internal HostDocument[] GetCurrentDocuments(IProjectSubscriptionUpdate update)
        {
            var documents = new List<HostDocument>();

            // Content Razor files
            if (update.CurrentState.TryGetValue(ContentItem.SchemaName, out var rule))
            {
                foreach (var kvp in rule.Items)
                {
                    if (TryGetRazorDocument(kvp.Value, out var document))
                    {
                        documents.Add(document);
                    }
                }
            }

            // None Razor files, these are typically included when a user links a file in Visual Studio.
            if (update.CurrentState.TryGetValue(NoneItem.SchemaName, out var nonRule))
            {
                foreach (var kvp in nonRule.Items)
                {
                    if (TryGetRazorDocument(kvp.Value, out var document))
                    {
                        documents.Add(document);
                    }
                }
            }

            return documents.ToArray();
        }

        // Internal for testing
        internal HostDocument[] GetChangedAndRemovedDocuments(IProjectSubscriptionUpdate update)
        {
            var documents = new List<HostDocument>();

            // Content Razor files
            if (update.ProjectChanges.TryGetValue(ContentItem.SchemaName, out var rule))
            {
                foreach (var key in rule.Difference.RemovedItems.Concat(rule.Difference.ChangedItems))
                {
                    if (rule.Before.Items.TryGetValue(key, out var value) &&
                        TryGetRazorDocument(value, out var document))
                    {
                        documents.Add(document);
                    }
                }
            }

            // None Razor files, these are typically included when a user links a file in Visual Studio.
            if (update.ProjectChanges.TryGetValue(NoneItem.SchemaName, out var nonRule))
            {
                foreach (var key in nonRule.Difference.RemovedItems.Concat(nonRule.Difference.ChangedItems))
                {
                    if (nonRule.Before.Items.TryGetValue(key, out var value) &&
                        TryGetRazorDocument(value, out var document))
                    {
                        documents.Add(document);
                    }
                }
            }

            return documents.ToArray();
        }

        // Internal for testing
        internal bool TryGetRazorDocument(IImmutableDictionary<string, string> itemState, out HostDocument razorDocument)
        {
            if (itemState.TryGetValue(ItemReference.FullPathPropertyName, out var filePath))
            {
                // If there's no target path then we normalize the target path to the file path. In the end, all we care about
                // is that the file being included in the primary project ends in .cshtml.
                itemState.TryGetValue(ItemReference.LinkPropertyName, out var targetPath);
                if (string.IsNullOrEmpty(targetPath))
                {
                    targetPath = filePath;
                }

                if (targetPath.EndsWith(".cshtml"))
                {
                    targetPath = CommonServices.UnconfiguredProject.MakeRooted(targetPath);
                    razorDocument = new HostDocument(filePath, targetPath);
                    return true;
                }
            }

            razorDocument = null;
            return false;
        }

        private static Version ReadAssemblyVersion(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new PEReader(stream))
                {
                    var metadataReader = reader.GetMetadataReader();

                    var assemblyDefinition = metadataReader.GetAssemblyDefinition();
                    return assemblyDefinition.Version;
                }
            }
            catch
            {
                // We're purposely silencing any kinds of I/O exceptions here, just in case something wacky is going on.
                return null;
            }
        }
    }
}