// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Extensions.ApiDescription.Tasks
{
    /// <summary>
    /// Adds or corrects ClassName, Namespace and OutputPath metadata in ServiceFileReference items. Also stores final
    /// metadata as SerializedMetadata.
    /// </summary>
    public class GetFileReferenceMetadata : Task
    {
        private const string TypeScriptLanguageName = "TypeScript";

        // Ignore Unicode escape sequences because, though they may escape valid class name characters,
        // backslash is not a valid filename character.
        private static readonly HashSet<UnicodeCategory> ValidClassNameCharacterCategories =
            new HashSet<UnicodeCategory>(new[]
            {
                // Formatting
                UnicodeCategory.Format, // Cf
                // Letter
                UnicodeCategory.LowercaseLetter, // Ll
                UnicodeCategory.ModifierLetter, // Lm
                UnicodeCategory.OtherLetter, // Lo
                UnicodeCategory.TitlecaseLetter, // Lt
                UnicodeCategory.UppercaseLetter, // Lu
                UnicodeCategory.LetterNumber, // Nl
                // Combining
                UnicodeCategory.SpacingCombiningMark, // Mc
                UnicodeCategory.NonSpacingMark, // Mn
                // Decimal digit
                UnicodeCategory.DecimalDigitNumber, // Nd
                // Connecting (includes underscore)
                UnicodeCategory.ConnectorPunctuation, // Pc
            });

        /// <summary>
        /// Extension to use in default OutputPath metadata value. Ignored when generating TypeScript.
        /// </summary>
        [Required]
        public string Extension { get; set; }

        /// <summary>
        /// Default Namespace metadata value.
        /// </summary>
        [Required]
        public string Namespace { get; set; }

        /// <summary>
        /// Default directory for OutputPath values.
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// The ServiceFileReference items to update.
        /// </summary>
        [Required]
        public ITaskItem[] Inputs { get; set; }

        /// <summary>
        /// The updated ServiceFileReference items. Will include ClassName, Namespace and OutputPath metadata.
        /// </summary>
        [Output]
        public ITaskItem[] Outputs{ get; set; }

        /// <inheritdoc />
        public override bool Execute()
        {
            var outputs = new List<ITaskItem>(Inputs.Length);
            var destinations = new HashSet<string>();

            foreach (var item in Inputs)
            {
                var newItem = new TaskItem(item);
                outputs.Add(newItem);

                var codeGenerator = item.GetMetadata("CodeGenerator");
                if (string.IsNullOrEmpty("CodeGenerator"))
                {
                    // This case occurs when user forgets to specify the required metadata. We have no default here.
                    string type;
                    if (!string.IsNullOrEmpty(item.GetMetadata("SourceProject")))
                    {
                        type = "ServiceProjectReference";
                    }
                    else if (!string.IsNullOrEmpty(item.GetMetadata("SourceUri")))
                    {
                        type = "ServiceUriReference";
                    }
                    else
                    {
                        type = "ServiceFileReference";
                    }

                    Log.LogError(Resources.FormatInvalidEmptyMetadataValue("CodeGenerator", type, item.ItemSpec));
                }

                var outputPath = item.GetMetadata("OutputPath");
                if (string.IsNullOrEmpty(outputPath))
                {
                    // No need to further sanitize this path.
                    var filename = item.GetMetadata("Filename");
                    var isTypeScript = codeGenerator.EndsWith(TypeScriptLanguageName, StringComparison.OrdinalIgnoreCase);
                    outputPath = $"{filename}Client{(isTypeScript ? ".ts" : Extension)}";
                }

                // Place output file in correct directory (relative to project directory).
                if (!Path.IsPathRooted(outputPath) && !string.IsNullOrEmpty(OutputDirectory))
                {
                    outputPath = Path.Combine(OutputDirectory, outputPath);
                }

                if (!destinations.Add(outputPath))
                {
                    // This case may occur when user is experimenting e.g. with multiple code generators or options.
                    // May also occur when user accidentally duplicates OutputPath metadata.
                    Log.LogError(Resources.FormatDuplicateFileOutputPaths(outputPath));
                }

                MetadataSerializer.SetMetadata(newItem, "OutputPath", outputPath);

                var className = item.GetMetadata("ClassName");
                if (string.IsNullOrEmpty(className))
                {
                    var outputFilename = Path.GetFileNameWithoutExtension(outputPath);
                    var classNameBuilder = new StringBuilder(outputFilename);

                    // Eliminate valid filename characters that are invalid in a class name e.g. '-'.
                    for (var index = 0; index < classNameBuilder.Length; index++)
                    {
                        var @char = classNameBuilder[index];
                        var category = char.GetUnicodeCategory(@char);
                        if (!ValidClassNameCharacterCategories.Contains(category))
                        {
                            classNameBuilder[index] = '_';
                        }
                        else if (index == 0 && @char != '_' && !char.IsLetter(@char))
                        {
                            classNameBuilder.Insert(0, '_');
                        }
                    }

                    className = classNameBuilder.ToString();
                    MetadataSerializer.SetMetadata(newItem, "ClassName", className);
                }

                var @namespace = item.GetMetadata("Namespace");
                if (string.IsNullOrEmpty(@namespace))
                {
                    MetadataSerializer.SetMetadata(newItem, "Namespace", Namespace);
                }

                // Add metadata which may be used as a property and passed to an inner build.
                newItem.RemoveMetadata("SerializedMetadata");
                newItem.SetMetadata("SerializedMetadata", MetadataSerializer.SerializeMetadata(newItem));
            }

            Outputs = outputs.ToArray();

            return !Log.HasLoggedErrors;
        }
    }
}
