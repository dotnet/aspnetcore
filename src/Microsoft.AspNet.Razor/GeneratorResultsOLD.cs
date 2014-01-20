// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.CodeDom;
using Microsoft.AspNet.Razor.Generator.Compiler;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor
{
    /// <summary>
    /// Represents results from code generation (and parsing, since that is a pre-requisite of code generation)
    /// </summary>
    /// <remarks>
    /// Since this inherits from ParserResults, it has all the data from ParserResults, and simply adds code generation data
    /// </remarks>
    public class GeneratorResultsOLD : ParserResults
    {
        public GeneratorResultsOLD(ParserResults parserResults,
                                CodeCompileUnit generatedCode,
                                IDictionary<int, GeneratedCodeMapping> designTimeLineMappings)
            : this(parserResults.Document, parserResults.ParserErrors, generatedCode, designTimeLineMappings)
        {
        }

        public GeneratorResultsOLD(Block document,
                                IList<RazorError> parserErrors,
                                CodeCompileUnit generatedCode,
                                IDictionary<int, GeneratedCodeMapping> designTimeLineMappings)
            : this(parserErrors.Count == 0, document, parserErrors, generatedCode, designTimeLineMappings)
        {
        }

        protected GeneratorResultsOLD(bool success,
                                   Block document,
                                   IList<RazorError> parserErrors,
                                   CodeCompileUnit generatedCode,
                                   IDictionary<int, GeneratedCodeMapping> designTimeLineMappings)
            : base(success, document, parserErrors)
        {
            GeneratedCode = generatedCode;
            DesignTimeLineMappings = designTimeLineMappings;
        }

        public CodeTree CodeTree { get; set; }

        /// <summary>
        /// The generated code
        /// </summary>
        public CodeCompileUnit GeneratedCode { get; private set; }

        /// <summary>
        /// If design-time mode was used in the Code Generator, this will contain the dictionary
        /// of design-time generated code mappings
        /// </summary>
        public IDictionary<int, GeneratedCodeMapping> DesignTimeLineMappings { get; private set; }
    }
}
