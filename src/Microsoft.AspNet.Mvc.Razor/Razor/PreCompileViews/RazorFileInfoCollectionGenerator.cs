// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorFileInfoCollectionGenerator
    {
        private string _fileFormat;

        protected IReadOnlyList<RazorFileInfo> FileInfos { get; private set; }
        protected CSharpParseOptions Options { get; private set; }

        public RazorFileInfoCollectionGenerator([NotNull] IReadOnlyList<RazorFileInfo> fileInfos,
                                                 [NotNull] CSharpParseOptions options)
        {
            FileInfos = fileInfos;
            Options = options;
        }

        public virtual SyntaxTree GenerateCollection()
        {
            var builder = new StringBuilder();
            builder.Append(Top);

            foreach (var fileInfo in FileInfos)
            {
                var perFileEntry = GenerateFile(fileInfo);
                builder.Append(perFileEntry);
            }

            builder.Append(Bottom);

            // TODO: consider saving the file for debuggability
            var sourceCode = builder.ToString();
            var syntaxTree = SyntaxTreeGenerator.Generate(sourceCode, 
                                                          "__AUTO__GeneratedViewsCollection.cs",
                                                          Options);

            return syntaxTree;
        }


        protected virtual string GenerateFile([NotNull] RazorFileInfo fileInfo)
        {
            return string.Format(FileFormat,
                                 fileInfo.LastModified.ToFileTimeUtc(),
                                 fileInfo.Length,
                                 fileInfo.RelativePath,
                                 fileInfo.FullTypeName,
                                 fileInfo.Hash);
        }

        protected virtual string Top
        {
            get
            {
                return 
@"using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Razor;

namespace __ASP_ASSEMBLY
{
    public class __PreGeneratedViewCollection : " + nameof(RazorFileInfoCollection) + @"
    {
        public __PreGeneratedViewCollection()
        {
            var fileInfos = new List<" + nameof(RazorFileInfo) + @">();
            " + nameof(RazorFileInfoCollection.FileInfos) + @" = fileInfos;
            " + nameof(RazorFileInfo) + @" info;

";
            }
        }

        protected virtual string Bottom
        {
            get
            {
                return
    @"        }
    }
}
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
                    "            info = new "
                    + nameof(RazorFileInfo) + @"
            {{
                " + nameof(RazorFileInfo.LastModified) + @" = DateTime.FromFileTimeUtc({0:D}),
                " + nameof(RazorFileInfo.Length) + @" = {1:D},
                " + nameof(RazorFileInfo.RelativePath) + @" = @""{2}"",
                " + nameof(RazorFileInfo.FullTypeName) + @" = @""{3}"",
                " + nameof(RazorFileInfo.Hash) + @" = @""{4}"",
            }};
            fileInfos.Add(info);
";
                }

                return _fileFormat;
            }
        }
    }
}