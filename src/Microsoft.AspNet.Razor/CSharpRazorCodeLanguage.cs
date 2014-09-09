// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.Parser;
#if NET45

#endif

namespace Microsoft.AspNet.Razor
{
    /// <summary>
    /// Defines the C# Code Language for Razor
    /// </summary>
    public class CSharpRazorCodeLanguage : RazorCodeLanguage
    {
        private const string CSharpLanguageName = "csharp";

        /// <summary>
        /// Returns the name of the language: "csharp"
        /// </summary>
        public override string LanguageName
        {
            get { return CSharpLanguageName; }
        }

        /// <summary>
        /// Constructs a new instance of the code parser for this language
        /// </summary>
        public override ParserBase CreateCodeParser()
        {
            return new CSharpCodeParser();
        }

        /// <summary>
        /// Constructs a new instance of the code generator for this language with the specified settings
        /// </summary>
        public override RazorCodeGenerator CreateCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
        {
            return new CSharpRazorCodeGenerator(className, rootNamespaceName, sourceFileName, host);
        }

        public override CodeBuilder CreateCodeBuilder(CodeBuilderContext context)
        {
            return new CSharpCodeBuilder(context);
        }
    }
}
