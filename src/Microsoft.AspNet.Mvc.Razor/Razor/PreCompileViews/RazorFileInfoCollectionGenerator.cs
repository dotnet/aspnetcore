// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Roslyn;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorFileInfoCollectionGenerator
    {
        private string _fileFormat;

        public RazorFileInfoCollectionGenerator([NotNull] RazorFileInfoCollection fileInfoCollection,
                                                [NotNull] CompilationSettings compilationSettings)
        {
            RazorFileInfoCollection = fileInfoCollection;
            CompilationSettings = compilationSettings;
        }

        protected RazorFileInfoCollection RazorFileInfoCollection { get; }

        protected CompilationSettings CompilationSettings { get; }

        public virtual SyntaxTree GenerateCollection()
        {
            var builder = new StringBuilder();
            builder.AppendFormat(CultureInfo.InvariantCulture,
                                 TopFormat,
                                 RazorFileInfoCollection.AssemblyResourceName,
                                 RazorFileInfoCollection.SymbolsResourceName);

            foreach (var fileInfo in RazorFileInfoCollection.FileInfos)
            {
                var perFileEntry = GenerateFile(fileInfo);
                builder.Append(perFileEntry);
            }

            builder.Append(Bottom);

            var sourceCode = builder.ToString();
            var syntaxTree = SyntaxTreeGenerator.Generate(sourceCode,
                                                          "__AUTO__GeneratedViewsCollection.cs",
                                                          CompilationSettings);

            return syntaxTree;
        }

        protected virtual string GenerateFile([NotNull] RazorFileInfo fileInfo)
        {
            return string.Format(FileFormat,
                                 fileInfo.LastModified.ToFileTime(),
                                 fileInfo.Length,
                                 fileInfo.RelativePath,
                                 fileInfo.FullTypeName,
                                 fileInfo.Hash,
                                 fileInfo.HashAlgorithmVersion);
        }

        protected virtual string TopFormat
        {
            get
            {
                return
$@"using System;
using System.Collections.Generic;
using System.Reflection;
using {typeof(RazorFileInfoCollection).Namespace};

namespace __ASP_ASSEMBLY
{{{{
    public class __PreGeneratedViewCollection : {nameof(RazorFileInfoCollection)}
    {{{{
        public __PreGeneratedViewCollection()
        {{{{
            {nameof(RazorFileInfoCollection.AssemblyResourceName)} = ""{{0}}"";
            {nameof(RazorFileInfoCollection.SymbolsResourceName)} = ""{{1}}"";
            var fileInfos = new List<{nameof(RazorFileInfo)}>();
            {nameof(RazorFileInfoCollection.FileInfos)} = fileInfos;
            {nameof(RazorFileInfo)} info;
";
            }
        }

        protected virtual string Bottom
        {
            get
            {
                return
    $@"        
        }}
        private static Assembly _loadedAssembly;

        public override Assembly LoadAssembly({typeof(IAssemblyLoadContext).FullName} loadContext)
        {{
             if (_loadedAssembly == null)
             {{
                _loadedAssembly = base.LoadAssembly(loadContext);
             }}
             return _loadedAssembly;   
        }}
    }}
}}
";
            }
        }

        protected virtual string FileFormat
        {
            get
            {
                if (_fileFormat == null)
                {
                    _fileFormat =
         $@"
            info = new {nameof(RazorFileInfo)}
            {{{{
                {nameof(RazorFileInfo.LastModified)} = DateTime.FromFileTimeUtc({{0:D}}).ToLocalTime(),
                {nameof(RazorFileInfo.Length)} = {{1:D}},
                {nameof(RazorFileInfo.RelativePath)} = @""{{2}}"",
                {nameof(RazorFileInfo.FullTypeName)} = @""{{3}}"",
                {nameof(RazorFileInfo.Hash)} = ""{{4}}"",
                {nameof(RazorFileInfo.HashAlgorithmVersion)} = {{5}},
            }}}};
            fileInfos.Add(info);
";
                }

                return _fileFormat;
            }
        }
    }
}