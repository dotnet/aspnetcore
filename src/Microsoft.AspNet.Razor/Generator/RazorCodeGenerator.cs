// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
                _context = new CodeGeneratorContext(Host, 
                                                    ClassName, 
                                                    RootNamespaceName, 
                                                    SourceFileName, 
                                                    GenerateLinePragmas);
                Initialize(_context);
            }
        }

        protected virtual void Initialize(CodeGeneratorContext context)
        {
        }
    }
}
