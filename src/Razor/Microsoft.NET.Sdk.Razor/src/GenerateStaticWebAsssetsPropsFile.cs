// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class GenerateStaticWebAsssetsPropsFile : Task
    {
        private const string SourceType = "SourceType";
        private const string SourceId = "SourceId";
        private const string ContentRoot = "ContentRoot";
        private const string BasePath = "BasePath";
        private const string RelativePath = "RelativePath";

        [Required]
        public string TargetPropsFilePath { get; set; }

        [Required]
        public ITaskItem[] StaticWebAssets { get; set; }

        public override bool Execute()
        {
            if (!ValidateArguments())
            {
                return false;
            }

            return ExecuteCore();
        }

        private bool ExecuteCore()
        {
            if (StaticWebAssets.Length == 0)
            {
                return !Log.HasLoggedErrors;
            }

            var template = StaticWebAssets[0];

            var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            var root = new XElement(
                "Project",
                new XElement("ItemGroup",
                    new XElement("StaticWebAsset",
                        new XAttribute("Include", @"$(MSBuildThisFileDirectory)..\staticwebassets\**"),
                        new XElement(SourceType, "Package"),
                        new XElement(SourceId, template.GetMetadata(SourceId)),
                        new XElement(ContentRoot, @"$(MSBuildThisFileDirectory)..\staticwebassets\"),
                        new XElement(BasePath, template.GetMetadata(BasePath)),
                        new XElement(RelativePath, "%(RecursiveDir)%(FileName)%(Extension)"))));

            document.Add(root);

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = true,
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineOnAttributes = false,
                Async = true
            };

            using (var xmlWriter = GetXmlWriter(settings))
            {
                document.WriteTo(xmlWriter);
            }

            return !Log.HasLoggedErrors;
        }

        private XmlWriter GetXmlWriter(XmlWriterSettings settings)
        {
            var fileStream = new FileStream(TargetPropsFilePath, FileMode.Create);
            return XmlWriter.Create(fileStream, settings);
        }

        private bool ValidateArguments()
        {
            ITaskItem firstAsset = null;

            for (var i = 0; i < StaticWebAssets.Length; i++)
            {
                var webAsset = StaticWebAssets[i];
                if (!EnsureRequiredMetadata(webAsset, SourceId) ||
                    !EnsureRequiredMetadata(webAsset, SourceType, allowEmpty: true) ||
                    !EnsureRequiredMetadata(webAsset, ContentRoot) ||
                    !EnsureRequiredMetadata(webAsset, BasePath) ||
                    !EnsureRequiredMetadata(webAsset, RelativePath))
                {
                    return false;
                }

                if (firstAsset == null)
                {
                    firstAsset = webAsset;
                    continue;
                }

                if (!ValidateMetadataMatches(firstAsset, webAsset, SourceId) ||
                    !ValidateMetadataMatches(firstAsset, webAsset, SourceType) ||
                    // Now that we support generated assets we need to be able to support multiple content roots.
                    // We need to change this check for one that ensures that no two files end up in the same final destination
                    //!ValidateMetadataMatches(firstAsset, webAsset, ContentRoot) ||
                    // See https://github.com/dotnet/aspnetcore/issues/24257
                    !ValidateMetadataMatches(firstAsset, webAsset, BasePath))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateMetadataMatches(ITaskItem reference, ITaskItem candidate, string metadata)
        {
            var referenceMetadata = reference.GetMetadata(metadata);
            var candidateMetadata = candidate.GetMetadata(metadata);
            if (!string.Equals(referenceMetadata, candidateMetadata, System.StringComparison.Ordinal))
            {
                Log.LogError($"Static web assets have different '{metadata}' metadata values '{referenceMetadata}' and '{candidateMetadata}' for '{reference.ItemSpec}' and '{candidate.ItemSpec}'.");
                return false;
            }

            return true;
        }

        private bool EnsureRequiredMetadata(ITaskItem item, string metadataName, bool allowEmpty = false)
        {
            var value = item.GetMetadata(metadataName);
            var isInvalidValue = allowEmpty ? !HasMetadata(item, metadataName) : string.IsNullOrEmpty(value);

            if (isInvalidValue)
            {
                Log.LogError($"Missing required metadata '{metadataName}' for '{item.ItemSpec}'.");
                return false;
            }

            return true;
        }

        private bool HasMetadata(ITaskItem item, string metadataName)
        {
            foreach (var name in item.MetadataNames)
            {
                if (string.Equals(metadataName, (string)name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
