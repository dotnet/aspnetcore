// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RepoTasks.ProjectModel;
using RepoTasks.Utilities;
using System.Text;

namespace RepoTasks
{
    public class GeneratePackageVersionPropsFile : Task
    {
        [Required]
        public ITaskItem[] Packages { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            OutputPath = OutputPath.Replace('\\', '/');
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            var props = new XElement(ns + "PropertyGroup");
            var root = new XElement(ns + "Project", props);
            var doc = new XDocument(root);

            props.Add(new XElement(ns + "MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)"));

            var varNames = new HashSet<string>();
            var versionElements = new List<XElement>();
            foreach (var pkg in Packages)
            {
                var packageVersion = pkg.GetMetadata("Version");

                if (string.IsNullOrEmpty(packageVersion))
                {
                    Log.LogError("Package {0} is missing the Version metadata", pkg.ItemSpec);
                    continue;
                }


                string packageVarName;
                if (!string.IsNullOrEmpty(pkg.GetMetadata("VariableName")))
                {
                    packageVarName = pkg.GetMetadata("VariableName");
                    if (!packageVarName.EndsWith("Version", StringComparison.Ordinal))
                    {
                        Log.LogError("VariableName for {0} must end in 'Version'", pkg.ItemSpec);
                        continue;
                    }
                }
                else
                {
                    packageVarName = GetVariableName(pkg.ItemSpec);
                }

                var packageTfm = pkg.GetMetadata("TargetFramework");
                var key = $"{packageVarName}/{packageTfm}";
                if (varNames.Contains(key))
                {
                    Log.LogError("Multiple packages would produce {0} in the generated dependencies.props file. Set VariableName to differentiate the packages manually", key);
                    continue;
                }
                varNames.Add(key);
                var elem = new XElement(ns + packageVarName, packageVersion);
                if (!string.IsNullOrEmpty(packageTfm))
                {
                    elem.Add(new XAttribute("Condition", $" '$(TargetFramework)' == '{packageTfm}' "));
                }
                versionElements.Add(elem);
            }

            foreach (var item in versionElements.OrderBy(p => p.Name.ToString()))
            {
                props.Add(item);
            }

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
            };
            using (var writer = XmlWriter.Create(OutputPath, settings))
            {
                Log.LogMessage(MessageImportance.Normal, $"Generate {OutputPath}");
                doc.Save(writer);
            }
            return !Log.HasLoggedErrors;
        }

        private string GetVariableName(string packageId)
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var ch in packageId)
            {
                if (ch == '.')
                {
                    first = true;
                    continue;
                }

                if (first)
                {
                    first = false;
                    sb.Append(char.ToUpperInvariant(ch));
                }
                else
                {
                    sb.Append(ch);
                }
            }
            sb.Append("PackageVersion");
            return sb.ToString();
        }
    }
}
