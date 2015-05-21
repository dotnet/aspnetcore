// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    public class ChunkGeneratorContext
    {
        protected ChunkGeneratorContext(ChunkGeneratorContext context)
            : this(
                context.Host,
                context.ClassName,
                context.RootNamespace,
                context.SourceFile,
                // True because we're pulling from the provided context's source file.
                shouldGenerateLinePragmas: true)
        {
            ChunkTreeBuilder = context.ChunkTreeBuilder;
        }

        public ChunkGeneratorContext(
            RazorEngineHost host,
            string className,
            string rootNamespace,
            string sourceFile,
            bool shouldGenerateLinePragmas)
        {
            ChunkTreeBuilder = new ChunkTreeBuilder();
            Host = host;
            SourceFile = shouldGenerateLinePragmas ? sourceFile : null;
            RootNamespace = rootNamespace;
            ClassName = className;
        }

        public string SourceFile { get; internal set; }

        public string RootNamespace { get; private set; }

        public string ClassName { get; private set; }

        public RazorEngineHost Host { get; private set; }

        public ChunkTreeBuilder ChunkTreeBuilder { get; set; }
    }
}
