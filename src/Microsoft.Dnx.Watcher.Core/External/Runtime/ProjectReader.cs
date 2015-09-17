// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Dnx.Runtime.Json;
using NuGet;

namespace Microsoft.Dnx.Runtime
{
    public class ProjectReader
    {
        public Project ReadProject(Stream stream, string projectName, string projectPath, ICollection<DiagnosticMessage> diagnostics)
        {
            var project = new Project();

            var reader = new StreamReader(stream);
            var rawProject = JsonDeserializer.Deserialize(reader) as JsonObject;
            if (rawProject == null)
            {
                throw FileFormatException.Create(
                    "The JSON file can't be deserialized to a JSON object.",
                    projectPath);
            }

            // Meta-data properties
            project.Name = projectName;
            project.ProjectFilePath = Path.GetFullPath(projectPath);

            var version = rawProject.Value("version") as JsonString;

            project.Description = rawProject.ValueAsString("description");
            project.Summary = rawProject.ValueAsString("summary");
            project.Copyright = rawProject.ValueAsString("copyright");
            project.Title = rawProject.ValueAsString("title");
            project.WebRoot = rawProject.ValueAsString("webroot");
            project.EntryPoint = rawProject.ValueAsString("entryPoint");
            project.ProjectUrl = rawProject.ValueAsString("projectUrl");
            project.LicenseUrl = rawProject.ValueAsString("licenseUrl");
            project.IconUrl = rawProject.ValueAsString("iconUrl");

            project.Authors = rawProject.ValueAsStringArray("authors") ?? new string[] { };
            project.Owners = rawProject.ValueAsStringArray("owners") ?? new string[] { };
            project.Tags = rawProject.ValueAsStringArray("tags") ?? new string[] { };

            project.Language = rawProject.ValueAsString("language");
            project.ReleaseNotes = rawProject.ValueAsString("releaseNotes");

            project.RequireLicenseAcceptance = rawProject.ValueAsBoolean("requireLicenseAcceptance", defaultValue: false);
            project.IsLoadable = rawProject.ValueAsBoolean("loadable", defaultValue: true);
            // TODO: Move this to the dependencies node
            project.EmbedInteropTypes = rawProject.ValueAsBoolean("embedInteropTypes", defaultValue: false);

            // Project files
            project.Files = new ProjectFilesCollection(rawProject, project.ProjectDirectory, project.ProjectFilePath);

            var commands = rawProject.Value("commands") as JsonObject;
            if (commands != null)
            {
                foreach (var key in commands.Keys)
                {
                    var value = commands.ValueAsString(key);
                    if (value != null)
                    {
                        project.Commands[key] = value;
                    }
                }
            }

            var scripts = rawProject.Value("scripts") as JsonObject;
            if (scripts != null)
            {
                foreach (var key in scripts.Keys)
                {
                    var stringValue = scripts.ValueAsString(key);
                    if (stringValue != null)
                    {
                        project.Scripts[key] = new string[] { stringValue };
                        continue;
                    }

                    var arrayValue = scripts.ValueAsStringArray(key);
                    if (arrayValue != null)
                    {
                        project.Scripts[key] = arrayValue;
                        continue;
                    }

                    throw FileFormatException.Create(
                        string.Format("The value of a script in {0} can only be a string or an array of strings", Project.ProjectFileName),
                        scripts.Value(key),
                        project.ProjectFilePath);
                }
            }

            return project;
        }

        private static SemanticVersion SpecifySnapshot(string version, string snapshotValue)
        {
            if (version.EndsWith("-*"))
            {
                if (string.IsNullOrEmpty(snapshotValue))
                {
                    version = version.Substring(0, version.Length - 2);
                }
                else
                {
                    version = version.Substring(0, version.Length - 1) + snapshotValue;
                }
            }

            return new SemanticVersion(version);
        }

        private static bool TryGetStringEnumerable(JsonObject parent, string property, out IEnumerable<string> result)
        {
            var collection = new List<string>();
            var valueInString = parent.ValueAsString(property);
            if (valueInString != null)
            {
                collection.Add(valueInString);
            }
            else
            {
                var valueInArray = parent.ValueAsStringArray(property);
                if (valueInArray != null)
                {
                    collection.AddRange(valueInArray);
                }
                else
                {
                    result = null;
                    return false;
                }
            }

            result = collection.SelectMany(value => value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries));
            return true;
        }
    }
}
