// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.CodeDom;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.Internal.Web.Utils;

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

        internal virtual Func<CodeWriter> CodeWriterFactory
        {
            get { return null; }
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

        public override void OnComplete()
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            Context.FlushBufferedStatement();
#endif
        }

        private void EnsureContextInitialized()
        {
            if (_context == null)
            {
                _context = CodeGeneratorContext.Create(Host, CodeWriterFactory, ClassName, RootNamespaceName, SourceFileName, GenerateLinePragmas);
                Initialize(_context);
            }
        }

        protected virtual void Initialize(CodeGeneratorContext context)
        {
#if NET45
            context.Namespace.Imports.AddRange(Host.NamespaceImports.Select(s => new CodeNamespaceImport(s)).ToArray());

            if (!String.IsNullOrEmpty(Host.DefaultBaseClass))
            {
                context.GeneratedClass.BaseTypes.Add(new CodeTypeReference(Host.DefaultBaseClass));
            }

            // Dev10 Bug 937438: Generate explicit Parameter-less constructor on Razor generated class
            context.GeneratedClass.Members.Add(new CodeConstructor() { Attributes = MemberAttributes.Public });
#endif
        }
    }
}
