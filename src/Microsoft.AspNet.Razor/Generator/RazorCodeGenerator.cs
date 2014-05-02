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
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public abstract class RazorCodeGenerator : ParserVisitor
    {
        private CodeGeneratorContext _context;

        protected RazorCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
        {
            if (String.IsNullOrEmpty(className))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "className");
            }
            if (rootNamespaceName == null)
            {
                throw new ArgumentNullException("rootNamespaceName");
            }
            if (host == null)
            {
                throw new ArgumentNullException("host");
            }

            ClassName = className;
            RootNamespaceName = rootNamespaceName;
            SourceFileName = sourceFileName;
            GenerateLinePragmas = String.IsNullOrEmpty(SourceFileName) ? false : true;
            Host = host;
        }

        // Data pulled from constructor
        public string ClassName { get; private set; }
        public string RootNamespaceName { get; private set; }
        public string SourceFileName { get; private set; }
        public RazorEngineHost Host { get; private set; }

        // Generation settings
        public bool GenerateLinePragmas { get; set; }
        public bool DesignTimeMode { get; set; }

        public CodeGeneratorContext Context
        {
            get
            {
                EnsureContextInitialized();
                return _context;
            }
        }

        public override void VisitStartBlock(Block block)
        {
            block.CodeGenerator.GenerateStartBlockCode(block, Context);
        }

        public override void VisitEndBlock(Block block)
        {
            block.CodeGenerator.GenerateEndBlockCode(block, Context);
        }

        public override void VisitSpan(Span span)
        {
            span.CodeGenerator.GenerateCode(span, Context);
        }

        private void EnsureContextInitialized()
        {
            if (_context == null)
            {
                _context = CodeGeneratorContext.Create(Host, ClassName, RootNamespaceName, SourceFileName, GenerateLinePragmas);
                Initialize(_context);
            }
        }

        protected virtual void Initialize(CodeGeneratorContext context)
        {
        }
    }
}
