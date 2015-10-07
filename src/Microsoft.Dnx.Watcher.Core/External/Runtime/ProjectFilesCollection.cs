// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.JsonParser.Sources;

namespace Microsoft.Dnx.Runtime
{
    public class ProjectFilesCollection
    {
        public static readonly string[] DefaultCompileBuiltInPatterns = new[] { @"**/*.cs" };
        public static readonly string[] DefaultPublishExcludePatterns = new[] { @"obj/**/*.*", @"bin/**/*.*", @"**/.*/**", @"**/global.json" };
        public static readonly string[] DefaultPreprocessPatterns = new[] { @"compiler/preprocess/**/*.cs" };
        public static readonly string[] DefaultSharedPatterns = new[] { @"compiler/shared/**/*.cs" };
        public static readonly string[] DefaultResourcesBuiltInPatterns = new[] { @"compiler/resources/**/*", "**/*.resx" };
        public static readonly string[] DefaultContentsBuiltInPatterns = new[] { @"**/*" };

        public static readonly string[] DefaultBuiltInExcludePatterns = new[] { "bin/**", "obj/**", "**/*.xproj" };

        public static readonly string PackIncludePropertyName = "packInclude";

        private PatternGroup _sharedPatternsGroup;
        private PatternGroup _resourcePatternsGroup;
        private PatternGroup _preprocessPatternsGroup;
        private PatternGroup _compilePatternsGroup;
        private PatternGroup _contentPatternsGroup;
        private IDictionary<string, string> _namedResources;
        private IEnumerable<string> _publishExcludePatterns;
        private IEnumerable<PackIncludeEntry> _packInclude;

        private readonly string _projectDirectory;
        private readonly string _projectFilePath;

        private JsonObject _rawProject;
        private bool _initialized;

        internal ProjectFilesCollection(JsonObject rawProject, string projectDirectory, string projectFilePath)
        {
            _projectDirectory = projectDirectory;
            _projectFilePath = projectFilePath;
            _rawProject = rawProject;
        }

        internal void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            var excludeBuiltIns = PatternsCollectionHelper.GetPatternsCollection(_rawProject, _projectDirectory, _projectFilePath, "excludeBuiltIn", DefaultBuiltInExcludePatterns);
            var excludePatterns = PatternsCollectionHelper.GetPatternsCollection(_rawProject, _projectDirectory, _projectFilePath, "exclude")
                                                          .Concat(excludeBuiltIns);
            var contentBuiltIns = PatternsCollectionHelper.GetPatternsCollection(_rawProject, _projectDirectory, _projectFilePath, "contentBuiltIn", DefaultContentsBuiltInPatterns);
            var compileBuiltIns = PatternsCollectionHelper.GetPatternsCollection(_rawProject, _projectDirectory, _projectFilePath, "compileBuiltIn", DefaultCompileBuiltInPatterns);
            var resourceBuiltIns = PatternsCollectionHelper.GetPatternsCollection(_rawProject, _projectDirectory, _projectFilePath, "resourceBuiltIn", DefaultResourcesBuiltInPatterns);

            _publishExcludePatterns = PatternsCollectionHelper.GetPatternsCollection(_rawProject, _projectDirectory, _projectFilePath, "publishExclude", DefaultPublishExcludePatterns);

            _sharedPatternsGroup = PatternGroup.Build(_rawProject, _projectDirectory, _projectFilePath, "shared", fallbackIncluding: DefaultSharedPatterns, additionalExcluding: excludePatterns);

            _resourcePatternsGroup = PatternGroup.Build(_rawProject, _projectDirectory, _projectFilePath, "resource", additionalIncluding: resourceBuiltIns, additionalExcluding: excludePatterns);

            _preprocessPatternsGroup = PatternGroup.Build(_rawProject, _projectDirectory, _projectFilePath, "preprocess", fallbackIncluding: DefaultPreprocessPatterns, additionalExcluding: excludePatterns)
                .ExcludeGroup(_sharedPatternsGroup)
                .ExcludeGroup(_resourcePatternsGroup);

            _compilePatternsGroup = PatternGroup.Build(_rawProject, _projectDirectory, _projectFilePath, "compile", additionalIncluding: compileBuiltIns, additionalExcluding: excludePatterns)
                .ExcludeGroup(_sharedPatternsGroup)
                .ExcludeGroup(_preprocessPatternsGroup)
                .ExcludeGroup(_resourcePatternsGroup);

            _contentPatternsGroup = PatternGroup.Build(_rawProject, _projectDirectory, _projectFilePath, "content", additionalIncluding: contentBuiltIns, additionalExcluding: excludePatterns.Concat(_publishExcludePatterns))
                .ExcludeGroup(_compilePatternsGroup)
                .ExcludeGroup(_preprocessPatternsGroup)
                .ExcludeGroup(_sharedPatternsGroup)
                .ExcludeGroup(_resourcePatternsGroup);

            _namedResources = NamedResourceReader.ReadNamedResources(_rawProject, _projectFilePath);

            // Files to be packed along with the project
            var packIncludeJson = _rawProject.ValueAsJsonObject(PackIncludePropertyName);
            if (packIncludeJson != null)
            {
                _packInclude = packIncludeJson
                    .Keys
                    .Select(k => new PackIncludeEntry(k, packIncludeJson.Value(k)))
                    .ToList();
            }
            else
            {
                _packInclude = new List<PackIncludeEntry>();
            }

            _initialized = true;
            _rawProject = null;
        }

        public IEnumerable<PackIncludeEntry> PackInclude
        {
            get
            {
                EnsureInitialized();
                return _packInclude;
            }
        }

        public IEnumerable<string> SourceFiles
        {
            get { return CompilePatternsGroup.SearchFiles(_projectDirectory).Distinct(); }
        }

        public IEnumerable<string> PreprocessSourceFiles
        {
            get { return PreprocessPatternsGroup.SearchFiles(_projectDirectory).Distinct(); }
        }

        public IDictionary<string, string> ResourceFiles
        {
            get
            {
                var resources = ResourcePatternsGroup
                    .SearchFiles(_projectDirectory)
                    .Distinct()
                    .ToDictionary(res => res, res => (string)null);

                NamedResourceReader.ApplyNamedResources(_namedResources, resources);

                return resources;
            }
        }

        public IEnumerable<string> SharedFiles
        {
            get { return SharedPatternsGroup.SearchFiles(_projectDirectory).Distinct(); }
        }

        public IEnumerable<string> GetFilesForBundling(bool includeSource, IEnumerable<string> additionalExcludePatterns)
        {
            var patternGroup = new PatternGroup(ContentPatternsGroup.IncludePatterns,
                                                ContentPatternsGroup.ExcludePatterns.Concat(additionalExcludePatterns),
                                                ContentPatternsGroup.IncludeLiterals);
            if (!includeSource)
            {
                foreach (var excludedGroup in ContentPatternsGroup.ExcludePatternsGroup)
                {
                    patternGroup.ExcludeGroup(excludedGroup);
                }
            }

            return patternGroup.SearchFiles(_projectDirectory);
        }

        internal PatternGroup CompilePatternsGroup
        {
            get
            {
                EnsureInitialized();
                return _compilePatternsGroup;
            }
        }

        internal PatternGroup SharedPatternsGroup
        {
            get
            {
                EnsureInitialized();
                return _sharedPatternsGroup;
            }
        }

        internal PatternGroup ResourcePatternsGroup
        {
            get
            {
                EnsureInitialized();
                return _resourcePatternsGroup;
            }
        }

        internal PatternGroup PreprocessPatternsGroup
        {
            get
            {
                EnsureInitialized();
                return _preprocessPatternsGroup;
            }
        }

        internal PatternGroup ContentPatternsGroup
        {
            get
            {
                EnsureInitialized();
                return _contentPatternsGroup;
            }
        }
    }
}
