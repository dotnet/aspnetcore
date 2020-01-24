// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Blazor.Build.Tasks
{
    // Based on https://github.com/mono/linker/blob/3b329b9481e300bcf4fb88a2eebf8cb5ef8b323b/src/ILLink.Tasks/LinkTask.cs
    public class BlazorILLink : ToolTask
    {
        private const string DotNetHostPathEnvironmentName = "DOTNET_HOST_PATH";

        [Required]
        public string ILLinkPath { get; set; }

        [Required]
        public ITaskItem[] AssemblyPaths { get; set; }

        public ITaskItem[] ReferenceAssemblyPaths { get; set; }

        [Required]
        public ITaskItem[] RootAssemblyNames { get; set; }

        [Required]
        public ITaskItem OutputDirectory { get; set; }

        public ITaskItem[] RootDescriptorFiles { get; set; }

        public bool ClearInitLocals { get; set; }

        public string ClearInitLocalsAssemblies { get; set; }

        public string ExtraArgs { get; set; }

        public bool DumpDependencies { get; set; }

        private string _dotnetPath;

        private string DotNetPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_dotnetPath))
                {
                    return _dotnetPath;
                }

                _dotnetPath = Environment.GetEnvironmentVariable(DotNetHostPathEnvironmentName);
                if (string.IsNullOrEmpty(_dotnetPath))
                {
                    throw new InvalidOperationException($"{DotNetHostPathEnvironmentName} is not set");
                }

                return _dotnetPath;
            }
        }

        protected override MessageImportance StandardErrorLoggingImportance => MessageImportance.High;

        protected override string ToolName => Path.GetFileName(DotNetPath);

        protected override string GenerateFullPathToTool() => DotNetPath;

        protected override string GenerateCommandLineCommands()
        {
            var args = new StringBuilder();
            args.Append(Quote(ILLinkPath));
            return args.ToString();
        }

        private static string Quote(string path)
        {
            return $"\"{path.TrimEnd('\\')}\"";
        }

        protected override string GenerateResponseFileCommands()
        {
            var args = new StringBuilder();

            if (RootDescriptorFiles != null)
            {
                foreach (var rootFile in RootDescriptorFiles)
                {
                    args.Append("-x ").AppendLine(Quote(rootFile.ItemSpec));
                }
            }

            foreach (var assemblyItem in RootAssemblyNames)
            {
                args.Append("-a ").AppendLine(Quote(assemblyItem.ItemSpec));
            }

            var assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in AssemblyPaths)
            {
                var assemblyPath = assembly.ItemSpec;
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

                // If there are multiple paths with the same assembly name, only use the first one.
                if (!assemblyNames.Add(assemblyName))
                {
                    continue;
                }

                args.Append("-reference ")
                    .AppendLine(Quote(assemblyPath));

                var action = assembly.GetMetadata("action");
                if ((action != null) && (action.Length > 0))
                {
                    args.Append("-p ");
                    args.Append(action);
                    args.Append(" ").AppendLine(Quote(assemblyName));
                }
            }

            if (ReferenceAssemblyPaths != null)
            {
                foreach (var assembly in ReferenceAssemblyPaths)
                {
                    var assemblyPath = assembly.ItemSpec;
                    var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

                    // Don't process references for which we already have
                    // implementation assemblies.
                    if (assemblyNames.Contains(assemblyName))
                    {
                        continue;
                    }

                    args.Append("-reference ").AppendLine(Quote(assemblyPath));

                    // Treat reference assemblies as "skip". Ideally we
                    // would not even look at the IL, but only use them to
                    // resolve surface area.
                    args.Append("-p skip ").AppendLine(Quote(assemblyName));
                }
            }

            if (OutputDirectory != null)
            {
                args.Append("-out ").AppendLine(Quote(OutputDirectory.ItemSpec));
            }

            if (ClearInitLocals)
            {
                args.AppendLine("--enable-opt clearinitlocals");
                if ((ClearInitLocalsAssemblies != null) && (ClearInitLocalsAssemblies.Length > 0))
                {
                    args.Append("-m ClearInitLocalsAssemblies ");
                    args.AppendLine(ClearInitLocalsAssemblies);
                }
            }

            if (ExtraArgs != null)
            {
                args.AppendLine(ExtraArgs);
            }

            if (DumpDependencies)
            {
                args.AppendLine("--dump-dependencies");
            }

            return args.ToString();
        }

        protected override bool HandleTaskExecutionErrors()
        {
            // Show a slightly better error than the standard ToolTask message that says "dotnet" failed.
            Log.LogError($"ILLink failed with exit code {ExitCode}.");
            return false;
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            if (!string.IsNullOrEmpty(singleLine) && singleLine.StartsWith("Unhandled exception.", StringComparison.Ordinal))
            {
                // The Mono linker currently prints out an entire stack trace when the linker fails.
                // We want to show something actionable in the VS Error window.
                Log.LogError(singleLine);
            }
            else
            {
                base.LogEventsFromTextOutput(singleLine, messageImportance);
            }
        }
    }
}
