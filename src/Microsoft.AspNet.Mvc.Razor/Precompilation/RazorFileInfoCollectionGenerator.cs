// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Text;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis;
using Microsoft.Dnx.Compilation.CSharp;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor.Precompilation
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
                                 fileInfo.RelativePath,
                                 fileInfo.FullTypeName);
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
                {nameof(RazorFileInfo.RelativePath)} = @""{{0}}"",
                {nameof(RazorFileInfo.FullTypeName)} = @""{{1}}""
            }}}};
            fileInfos.Add(info);
";
                }

                return _fileFormat;
            }
        }
    }
}