// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.AspNetCore.Tools;
using Microsoft.Extensions.Tools.Internal;

internal static class UserSecretsCreator
{
    public static string CreateUserSecretsId(IReporter reporter, string project, string workingDirectory, string overrideId = null)
    {
        var projectPath = ResolveProjectPath(project, workingDirectory);

        // Load the project file as XML
        var projectDocument = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);

        // Accept the `--id` CLI option to the main app
        string newSecretsId = string.IsNullOrWhiteSpace(overrideId)
            ? Guid.NewGuid().ToString()
            : overrideId;

        // Confirm secret ID does not contain invalid characters
        if (Path.GetInvalidPathChars().Any(newSecretsId.Contains))
        {
            throw new ArgumentException(SecretsHelpersResources.FormatError_InvalidSecretsId(newSecretsId));
        }

        var existingUserSecretsId = projectDocument.XPathSelectElements("//UserSecretsId").FirstOrDefault();

        // Check if a UserSecretsId is already set
        if (existingUserSecretsId is not null)
        {
            // Only set the UserSecretsId if the user specified an explicit value
            if (string.IsNullOrWhiteSpace(overrideId))
            {
                reporter.Output(SecretsHelpersResources.FormatMessage_ProjectAlreadyInitialized(projectPath));
                return existingUserSecretsId.Value;
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
            propertyGroup.Add("  ");
            propertyGroup.Add(new XElement("UserSecretsId", newSecretsId));
            propertyGroup.Add($"{Environment.NewLine}  ");
        }

        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
        };

        using var xw = XmlWriter.Create(projectPath, settings);
        projectDocument.Save(xw);

        reporter.Output(SecretsHelpersResources.FormatMessage_SetUserSecretsIdForProject(newSecretsId, projectPath));
        return newSecretsId;
    }

    private static string ResolveProjectPath(string name, string path)
    {
        var finder = new MsBuildProjectFinder(path);
        return finder.FindMsBuildProject(name);
    }
}
