// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using MonoDevelop.Projects;
using AssemblyReference = MonoDevelop.Projects.AssemblyReference;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.ProjectSystem
{
    internal class FallbackRazorProjectHost : RazorProjectHostBase
    {
        private const string MvcAssemblyFileName = "Microsoft.AspNetCore.Mvc.Razor.dll";

        public FallbackRazorProjectHost(
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
                var referencedAssemblies = await DotNetProject.GetReferencedAssemblies(ConfigurationSelector.Default);
                var mvcReference = referencedAssemblies.FirstOrDefault(IsMvcAssembly);

                if (mvcReference == null)
                {
                    // Ok we can't find an MVC version. Let's assume this project isn't using Razor then.
                    await UpdateHostProjectUnsafeAsync(null).ConfigureAwait(false);
                    return;
                }

                var version = GetAssemblyVersion(mvcReference.FilePath);
                if (version == null)
                {
                    // Ok we can't find an MVC version. Let's assume this project isn't using Razor then.
                    await UpdateHostProjectUnsafeAsync(null).ConfigureAwait(false);
                    return;
                }

                var configuration = FallbackRazorConfiguration.SelectConfiguration(version);
                var hostProject = new HostProject(DotNetProject.FileName.FullPath, configuration);
                await UpdateHostProjectUnsafeAsync(hostProject).ConfigureAwait(false);
            });
        }

        // Internal for testing
        internal static bool IsMvcAssembly(AssemblyReference reference)
        {
            var fileName = reference?.FilePath.FileName;

            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            if (string.Equals(reference.FilePath.FileName, MvcAssemblyFileName, StringComparison.OrdinalIgnoreCase))
            {
                // Mvc assembly
                return true;
            }

            return false;
        }

        private static Version GetAssemblyVersion(string filePath)
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
