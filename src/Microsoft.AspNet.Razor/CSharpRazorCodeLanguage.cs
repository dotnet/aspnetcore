// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.CSharp;

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
#if NET45
        // No CodeDOM in CoreCLR

        /// <summary>
        /// Returns the type of the CodeDOM provider for this language
        /// </summary>
        public override Type CodeDomProviderType
        {
            get { return typeof(CSharpCodeProvider); }
        }
#endif
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

        public override CodeBuilder CreateBuilder(CodeGeneratorContext codeGeneratorContext)
        {
            return new CSharpCodeBuilder(codeGeneratorContext);
        }
    }
}
