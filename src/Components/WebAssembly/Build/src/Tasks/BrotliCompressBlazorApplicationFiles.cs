// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class BrotliCompressBlazorApplicationFiles : ToolTask
    {
        private const string DotNetHostPathEnvironmentName = "DOTNET_HOST_PATH";

        [Required]
        public string ManifestPath { get; set; }

        [Required]
        public string BlazorBrotliPath { get; set; }

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

        protected override string GenerateCommandLineCommands() =>
            $"\"{BlazorBrotliPath}\" \"{ManifestPath}\"";
    }
}
