// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Framework.FileSystemGlobbing;
using Microsoft.Dnx.Runtime.Json;

namespace Microsoft.Dnx.Runtime
{
    public class PatternGroup
    {
        private readonly List<PatternGroup> _excludeGroups = new List<PatternGroup>();
        private readonly Matcher _matcher = new Matcher();

        internal PatternGroup(IEnumerable<string> includePatterns)
        {
            IncludeLiterals = Enumerable.Empty<string>();
            IncludePatterns = includePatterns;
            ExcludePatterns = Enumerable.Empty<string>();
            _matcher.AddIncludePatterns(IncludePatterns);
        }

        internal PatternGroup(IEnumerable<string> includePatterns, IEnumerable<string> excludePatterns, IEnumerable<string> includeLiterals)
        {
            IncludeLiterals = includeLiterals;
            IncludePatterns = includePatterns;
            ExcludePatterns = excludePatterns;

            _matcher.AddIncludePatterns(IncludePatterns);
            _matcher.AddExcludePatterns(ExcludePatterns);
        }

        internal static PatternGroup Build(JsonObject rawProject,
                                           string projectDirectory,
                                           string projectFilePath,
                                           string name,
                                           IEnumerable<string> fallbackIncluding = null,
                                           IEnumerable<string> additionalIncluding = null,
                                           IEnumerable<string> additionalExcluding = null,
                                           bool includePatternsOnly = false,
                                           ICollection<DiagnosticMessage> warnings = null)
        {
            string includePropertyName = name;
            additionalIncluding = additionalIncluding ?? Enumerable.Empty<string>();
            var includePatterns = PatternsCollectionHelper.GetPatternsCollection(rawProject, projectDirectory, projectFilePath, includePropertyName, defaultPatterns: fallbackIncluding)
                                                          .Concat(additionalIncluding)
                                                          .Distinct();

            if (includePatternsOnly)
            {
                return new PatternGroup(includePatterns);
            }

            additionalExcluding = additionalExcluding ?? Enumerable.Empty<string>();
            var excludePatterns = PatternsCollectionHelper.GetPatternsCollection(rawProject, projectDirectory, projectFilePath, propertyName: name + "Exclude")
                                                          .Concat(additionalExcluding)
                                                          .Distinct();

            var includeLiterals = PatternsCollectionHelper.GetPatternsCollection(rawProject, projectDirectory, projectFilePath, propertyName: name + "Files", literalPath: true)
                                                          .Distinct();

            return new PatternGroup(includePatterns, excludePatterns, includeLiterals);
        }

        public IEnumerable<string> IncludeLiterals { get; }

        public IEnumerable<string> IncludePatterns { get; }

        public IEnumerable<string> ExcludePatterns { get; }

        public IEnumerable<PatternGroup> ExcludePatternsGroup { get { return _excludeGroups; } }

        public PatternGroup ExcludeGroup(PatternGroup group)
        {
            _excludeGroups.Add(group);

            return this;
        }

        public IEnumerable<string> SearchFiles(string rootPath)
        {
            // literal included files are added at the last, but the search happens early
            // so as to make the process fail early in case there is missing file. fail early
            // helps to avoid unnecessary globing for performance optimization
            var literalIncludedFiles = new List<string>();
            foreach (var literalRelativePath in IncludeLiterals)
            {
                var fullPath = Path.GetFullPath(Path.Combine(rootPath, literalRelativePath));

                if (!File.Exists(fullPath))
                {
                    throw new InvalidOperationException(string.Format("Can't find file {0}", literalRelativePath));
                }

                // TODO: extract utility like NuGet.PathUtility.GetPathWithForwardSlashes()
                literalIncludedFiles.Add(fullPath.Replace('\\', '/'));
            }

            // globing files
            var globbingResults = _matcher.GetResultsInFullPath(rootPath);

            // if there is no results generated in globing, skip excluding other groups 
            // for performance optimization.
            if (globbingResults.Any())
            {
                foreach (var group in _excludeGroups)
                {
                    globbingResults = globbingResults.Except(group.SearchFiles(rootPath));
                }
            }

            return globbingResults.Concat(literalIncludedFiles).Distinct();
        }

        public override string ToString()
        {
            return string.Format("Pattern group: Literals [{0}] Includes [{1}] Excludes [{2}]", string.Join(", ", IncludeLiterals), string.Join(", ", IncludePatterns), string.Join(", ", ExcludePatterns));
        }
    }
}