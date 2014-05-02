// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor
{
    public class GeneratorResults : ParserResults
    {
        public GeneratorResults(ParserResults parserResults,
                        CodeBuilderResult codeBuilderResult)
            : this(parserResults.Document, parserResults.ParserErrors, codeBuilderResult)
        {
        }

        public GeneratorResults(Block document,
                                IList<RazorError> parserErrors,
                                CodeBuilderResult codeBuilderResult)
            : this(parserErrors.Count == 0, document, parserErrors, codeBuilderResult)
        {
        }

        protected GeneratorResults(bool success,
                                   Block document,
                                   IList<RazorError> parserErrors,
                                   CodeBuilderResult codeBuilderResult)
            : base(success, document, parserErrors)
        {
            GeneratedCode = codeBuilderResult.Code;
            DesignTimeLineMappings = codeBuilderResult.DesignTimeLineMappings;
        }

        public string GeneratedCode { get; private set; }
        public IList<LineMapping> DesignTimeLineMappings { get; private set; }
    }
}
