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

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Razor
{
    /// <summary>
    /// Represents a code language in Razor.
    /// </summary>
    public abstract class RazorCodeLanguage
    {
        private static IDictionary<string, RazorCodeLanguage> _services = new Dictionary<string, RazorCodeLanguage>(StringComparer.OrdinalIgnoreCase)
        {
            { "cshtml", new CSharpRazorCodeLanguage() }
        };

        /// <summary>
        /// Gets the list of registered languages mapped to file extensions (without a ".")
        /// </summary>
        public static IDictionary<string, RazorCodeLanguage> Languages
        {
            get { return _services; }
        }

        /// <summary>
        /// The name of the language (for use in System.Web.Compilation.BuildProvider.GetDefaultCompilerTypeForLanguage)
        /// </summary>
        public abstract string LanguageName { get; }

        /// <summary>
        /// Gets the RazorCodeLanguage registered for the specified file extension
        /// </summary>
        /// <param name="fileExtension">The extension, with or without a "."</param>
        /// <returns>The language registered for that extension</returns>
        public static RazorCodeLanguage GetLanguageByExtension(string fileExtension)
        {
            RazorCodeLanguage service = null;
            Languages.TryGetValue(fileExtension.TrimStart('.'), out service);
            return service;
        }

        /// <summary>
        /// Constructs the code parser.  Must return a new instance on EVERY call to ensure thread-safety
        /// </summary>
        public abstract ParserBase CreateCodeParser();

        /// <summary>
        /// Constructs the code generator.  Must return a new instance on EVERY call to ensure thread-safety
        /// </summary>
        public abstract RazorCodeGenerator CreateCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host);

        public abstract CodeBuilder CreateCodeBuilder(CodeGeneratorContext codeGeneratorContext);
    }
}
