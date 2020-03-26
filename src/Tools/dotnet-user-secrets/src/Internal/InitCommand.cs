// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    /// <remarks>
    /// Workaround used to handle the fact that the options have not been parsed at configuration time
    /// </remarks>
    public class InitCommandFactory : ICommand
    {
        public CommandLineOptions Options { get; }

        internal static void Configure(CommandLineApplication command, CommandLineOptions options)
        {
            command.Description = "Set a user secrets ID to enable secret storage";
            command.HelpOption();

            command.OnExecute(() =>
            {
                options.Command = new InitCommandFactory(options);
            });
        }

        public InitCommandFactory(CommandLineOptions options)
        {
            Options = options;
        }

        public void Execute(CommandContext context)
        {
            new InitCommand(Options.Id, Options.Project).Execute(context);
        }

        public void Execute(CommandContext context, string workingDirectory)
        {
            new InitCommand(Options.Id, Options.Project).Execute(context, workingDirectory);
        }
    }

    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class InitCommand : ICommand
    {
        public string OverrideId { get; }
        public string ProjectPath { get; }
        public string WorkingDirectory { get; private set; } = Directory.GetCurrentDirectory();

        public InitCommand(string id, string project)
        {
            OverrideId = id;
            ProjectPath = project;
        }

        public void Execute(CommandContext context, string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
            Execute(context);
        }

        public void Execute(CommandContext context)
        {
            var projectPath = ResolveProjectPath(ProjectPath, WorkingDirectory);

            // Load the project file as XML
            var projectDocument = XDocument.Load(projectPath);

            // Accept the `--id` CLI option to the main app
            string newSecretsId = string.IsNullOrWhiteSpace(OverrideId)
                ? Guid.NewGuid().ToString()
                : OverrideId;

            // Confirm secret ID does not contain invalid characters
            if (Path.GetInvalidPathChars().Any(invalidChar => newSecretsId.Contains(invalidChar)))
            {
                throw new ArgumentException(Resources.FormatError_InvalidSecretsId(newSecretsId));
            }

            var existingUserSecretsId = projectDocument.XPathSelectElements("//UserSecretsId").FirstOrDefault();

            // Check if a UserSecretsId is already set
            if (existingUserSecretsId is object)
            {
                // Only set the UserSecretsId if the user specified an explicit value
                if (string.IsNullOrWhiteSpace(OverrideId))
                {
                    context.Reporter.Output(Resources.FormatMessage_ProjectAlreadyInitialized(projectPath));
                    return;
                }

                existingUserSecretsId.SetValue(newSecretsId);
            }
            else
            {
                // Find the first non-conditional PropertyGroup
                var propertyGroup = projectDocument.Root.DescendantNodes()
                    .FirstOrDefault(node => node is XElement el
                        && el.Name == "PropertyGroup"
                        && el.Attributes().All(attr =>
                            attr.Name != "Condition")) as XElement;

                // No valid property group, create a new one
                if (propertyGroup == null)
                {
                    propertyGroup = new XElement("PropertyGroup");
                    projectDocument.Root.AddFirst(propertyGroup);
                }

                // Add UserSecretsId element
                propertyGroup.Add(new XElement("UserSecretsId", newSecretsId));
            }

            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true,
            };

            using (var xw = XmlWriter.Create(projectPath, settings))
            {
                projectDocument.Save(xw);
            }

            context.Reporter.Output(Resources.FormatMessage_SetUserSecretsIdForProject(newSecretsId, projectPath));
        }

        private static string ResolveProjectPath(string name, string path)
        {
            var finder = new MsBuildProjectFinder(path);
            return finder.FindMsBuildProject(name);
        }
    }
}
