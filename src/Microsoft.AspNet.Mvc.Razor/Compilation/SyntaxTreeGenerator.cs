// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNet.Mvc.Razor
{
    public static class SyntaxTreeGenerator
    {
        private static CSharpParseOptions DefaultOptions
        {
            get
            {
                return CSharpParseOptions.Default
                        .WithLanguageVersion(LanguageVersion.CSharp6);
            }
        }

        public static SyntaxTree Generate([NotNull] string text, [NotNull] string path)
        {
            return GenerateCore(text, path, DefaultOptions);
        }

        public static SyntaxTree Generate([NotNull] string text,
                                          [NotNull] string path,
                                          [NotNull] CSharpParseOptions options)
        {
            return GenerateCore(text, path, options);
        }

        public static SyntaxTree GenerateCore([NotNull] string text,
                                              [NotNull] string path,
                                              [NotNull] CSharpParseOptions options)
        {
            var sourceText = SourceText.From(text, Encoding.UTF8);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText,
                path: path,
                options: options);

            return syntaxTree;
        }

        public static CSharpParseOptions GetParseOptions(CSharpCompilation compilation)
        {
            return CSharpParseOptions.Default
                              .WithLanguageVersion(compilation.LanguageVersion);
        }
    }
}