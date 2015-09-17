// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;

namespace Microsoft.Dnx.Runtime
{
    public class Project
    {
        public const string ProjectFileName = "project.json";

        public Project()
        {
        }

        public string ProjectFilePath { get; set; }

        public string ProjectDirectory
        {
            get
            {
                return Path.GetDirectoryName(ProjectFilePath);
            }
        }

        public string Name { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Copyright { get; set; }

        public string Summary { get; set; }

        public string Language { get; set; }

        public string ReleaseNotes { get; set; }

        public string[] Authors { get; set; }

        public string[] Owners { get; set; }

        public bool EmbedInteropTypes { get; set; }

        public Version AssemblyFileVersion { get; set; }
        public string WebRoot { get; set; }

        public string EntryPoint { get; set; }

        public string ProjectUrl { get; set; }

        public string LicenseUrl { get; set; }

        public string IconUrl { get; set; }

        public bool RequireLicenseAcceptance { get; set; }

        public string[] Tags { get; set; }

        public bool IsLoadable { get; set; }

        public ProjectFilesCollection Files { get; set; }

        public IDictionary<string, string> Commands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public IDictionary<string, IEnumerable<string>> Scripts { get; } = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

        public static bool HasProjectFile(string path)
        {
            string projectPath = Path.Combine(path, ProjectFileName);

            return File.Exists(projectPath);
        }

        public static bool TryGetProject(string path, out Project project, ICollection<DiagnosticMessage> diagnostics = null)
        {
            project = null;

            string projectPath = null;

            if (string.Equals(Path.GetFileName(path), ProjectFileName, StringComparison.OrdinalIgnoreCase))
            {
                projectPath = path;
                path = Path.GetDirectoryName(path);
            }
            else if (!HasProjectFile(path))
            {
                return false;
            }
            else
            {
                projectPath = Path.Combine(path, ProjectFileName);
            }

            // Assume the directory name is the project name if none was specified
            var projectName = PathUtility.GetDirectoryName(path);
            projectPath = Path.GetFullPath(projectPath);

            if (!File.Exists(projectPath))
            {
                return false;
            }

            try
            {
                using (var stream = File.OpenRead(projectPath))
                {
                    var reader = new ProjectReader();
                    project = reader.ReadProject(stream, projectName, projectPath, diagnostics);
                }
            }
            catch (Exception ex)
            {
                throw FileFormatException.Create(ex, projectPath);
            }

            return true;
        }
    }
}
