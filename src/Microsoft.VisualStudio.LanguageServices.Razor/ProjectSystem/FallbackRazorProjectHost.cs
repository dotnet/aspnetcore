// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem;
using ResolvedCompilationReference = Microsoft.CodeAnalysis.Razor.ProjectSystem.ManageProjectSystemSchema.ResolvedCompilationReference;

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
                ruleNames: new string[] { ResolvedCompilationReference.SchemaName },
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
                    var references = update.Value.CurrentState[ResolvedCompilationReference.SchemaName].Items;
                    foreach (var reference in references)
                    {
                        if (reference.Key.EndsWith(MvcAssemblyFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            mvcReferenceFullPath = reference.Key;
                            break;
                        }
                    }

                    if (mvcReferenceFullPath == null)
                    {
                        // Ok we can't find an MVC version. Let's assume this project isn't using Razor then.
                        await UpdateProjectUnsafeAsync(null).ConfigureAwait(false);
                        return;
                    }

                    var version = GetAssemblyVersion(mvcReferenceFullPath);
                    if (version == null)
                    {
                        // Ok we can't find an MVC version. Let's assume this project isn't using Razor then.
                        await UpdateProjectUnsafeAsync(null).ConfigureAwait(false);
                        return;
                    }

                    var configuration = FallbackRazorConfiguration.SelectConfiguration(version);
                    var hostProject = new HostProject(CommonServices.UnconfiguredProject.FullPath, configuration);
                    await UpdateProjectUnsafeAsync(hostProject).ConfigureAwait(false);
                });
            }, registerFaultHandler: true);
        }

        // virtual for overriding in tests
        protected virtual Version GetAssemblyVersion(string filePath)
        {
            return ReadAssemblyVersion(filePath);
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