// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor
{
    public class GeneratorResults : ParserResults
    {
        public GeneratorResults(ParserResults parserResults, CodeBuilderResult codeBuilderResult, CodeTree codeTree)
            : this(parserResults.Document, 
                   parserResults.ParserErrors, 
                   codeBuilderResult,
                   codeTree)
        {
        }

        public GeneratorResults(Block document,
                                IList<RazorError> parserErrors,
                                CodeBuilderResult codeBuilderResult,
                                CodeTree codeTree)
            : this(parserErrors.Count == 0, document, parserErrors, codeBuilderResult, codeTree)
        {
        }

        protected GeneratorResults(bool success,
                                   Block document,
                                   IList<RazorError> parserErrors,
                                   CodeBuilderResult codeBuilderResult,
                                   CodeTree codeTree)
            : base(success, document, parserErrors)
        {
            GeneratedCode = codeBuilderResult.Code;
            DesignTimeLineMappings = codeBuilderResult.DesignTimeLineMappings;
            CodeTree = codeTree;
        }

        public string GeneratedCode { get; private set; }

        public IList<LineMapping> DesignTimeLineMappings { get; private set; }

        public CodeTree CodeTree { get; private set; }
    }
}
