// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.VisualBasic;
using System;

namespace Microsoft.AspNet.Razor
{
    /// <summary>
    /// Defines the Visual Basic Code Language for Razor
    /// </summary>
    public class VBRazorCodeLanguage : RazorCodeLanguage
    {
        private const string VBLanguageName = "vb";

        /// <summary>
        /// Returns the name of the language: "vb"
        /// </summary>
        public override string LanguageName
        {
            get { return VBLanguageName; }
        }

        /// <summary>
        /// Returns the type of the CodeDOM provider for this language
        /// </summary>
        public override Type CodeDomProviderType
        {
            get { return typeof(VBCodeProvider); }
        }

        /// <summary>
        /// Constructs a new instance of the code parser for this language
        /// </summary>
        public override ParserBase CreateCodeParser()
        {
            return new VBCodeParser();
        }

        /// <summary>
        /// Constructs a new instance of the code generator for this language with the specified settings
        /// </summary>
        public override RazorCodeGenerator CreateCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
        {
            return new VBRazorCodeGenerator(className, rootNamespaceName, sourceFileName, host);
        }
    }
}
