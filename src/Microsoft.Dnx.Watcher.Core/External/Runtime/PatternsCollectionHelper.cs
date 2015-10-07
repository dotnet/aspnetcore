// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.JsonParser.Sources;

namespace Microsoft.Dnx.Runtime
{
    internal static class PatternsCollectionHelper
    {
        private static readonly char[] PatternSeparator = new[] { ';' };

        public static IEnumerable<string> GetPatternsCollection(
            JsonObject rawProject,
            string projectDirectory,
            string projectFilePath,
            string propertyName,
            IEnumerable<string> defaultPatterns = null,
            bool literalPath = false)
        {
            defaultPatterns = defaultPatterns ?? Enumerable.Empty<string>();

            try
            {
                if (!rawProject.Keys.Contains(propertyName))
                {
                    return CreateCollection(projectDirectory, propertyName, defaultPatterns, literalPath);
                }

                var valueInString = rawProject.ValueAsString(propertyName);
                if (valueInString != null)
                {
                    return CreateCollection(projectDirectory, propertyName, new string[] { valueInString }, literalPath);
                }

                var valuesInArray = rawProject.ValueAsStringArray(propertyName);
                if (valuesInArray != null)
                {
                    return CreateCollection(projectDirectory, propertyName, valuesInArray.Select(s => s.ToString()), literalPath);
                }
            }
            catch (Exception ex)
            {
                throw FileFormatException.Create(ex, rawProject.Value(propertyName), projectFilePath);
            }

            throw FileFormatException.Create("Value must be either string or array.", rawProject.Value(propertyName), projectFilePath);
        }

        private static IEnumerable<string> CreateCollection(string projectDirectory, string propertyName, IEnumerable<string> patternsStrings, bool literalPath)
        {
            var patterns = patternsStrings.SelectMany(patternsString => GetSourcesSplit(patternsString))
                                          .Select(patternString => patternString.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));

            foreach (var pattern in patterns)
            {
                if (Path.IsPathRooted(pattern))
                {
                    throw new InvalidOperationException($"The '{propertyName}' property cannot be a rooted path.");
                }

                if (literalPath && pattern.Contains('*'))
                {
                    throw new InvalidOperationException($"The '{propertyName}' property cannot contain wildcard characters.");
                }
            }

            return new List<string>(patterns.Select(pattern => FolderToPattern(pattern, projectDirectory)));
        }

        private static IEnumerable<string> GetSourcesSplit(string sourceDescription)
        {
            if (string.IsNullOrEmpty(sourceDescription))
            {
                return Enumerable.Empty<string>();
            }

            return sourceDescription.Split(PatternSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string FolderToPattern(string candidate, string projectDir)
        {
            // This conversion is needed to support current template

            // If it's already a pattern, no change is needed
            if (candidate.Contains('*'))
            {
                return candidate;
            }

            // If the given string ends with a path separator, or it is an existing directory
            // we convert this folder name to a pattern matching all files in the folder
            if (candidate.EndsWith(@"\") ||
                candidate.EndsWith("/") ||
                Directory.Exists(Path.Combine(projectDir, candidate)))
            {
                return Path.Combine(candidate, "**", "*");
            }

            // Otherwise, it represents a single file
            return candidate;
        }
    }
}
