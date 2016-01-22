// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    public class RazorChunkGenerator : ParserVisitor
    {
        private ChunkGeneratorContext _context;

        public RazorChunkGenerator(
            string className,
            string rootNamespaceName,
            string sourceFileName,
            RazorEngineHost host)
        {
            if (rootNamespaceName == null)
            {
                throw new ArgumentNullException(nameof(rootNamespaceName));
            }

            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, nameof(className));
            }

            ClassName = className;
            RootNamespaceName = rootNamespaceName;
            SourceFileName = sourceFileName;
            GenerateLinePragmas = string.IsNullOrEmpty(SourceFileName) ? false : true;
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

        public ChunkGeneratorContext Context
        {
            get
            {
                EnsureContextInitialized();
                return _context;
            }
        }

        public override void VisitStartBlock(Block block)
        {
            block.ChunkGenerator.GenerateStartParentChunk(block, Context);
        }

        public override void VisitEndBlock(Block block)
        {
            block.ChunkGenerator.GenerateEndParentChunk(block, Context);
        }

        public override void VisitSpan(Span span)
        {
            span.ChunkGenerator.GenerateChunk(span, Context);
        }

        private void EnsureContextInitialized()
        {
            if (_context == null)
            {
                _context = new ChunkGeneratorContext(Host,
                                                    ClassName,
                                                    RootNamespaceName,
                                                    SourceFileName,
                                                    GenerateLinePragmas);
                Initialize(_context);
            }
        }

        protected virtual void Initialize(ChunkGeneratorContext context)
        {
        }
    }
}
