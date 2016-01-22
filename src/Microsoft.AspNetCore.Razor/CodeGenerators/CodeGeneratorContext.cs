// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks.Generators;

namespace Microsoft.AspNet.Razor.CodeGenerators
{
    /// <summary>
    /// Context object with information used to generate a Razor page.
    /// </summary>
    public class CodeGeneratorContext : ChunkGeneratorContext
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="CodeGeneratorContext"/> object.
        /// </summary>
        /// <param name="generatorContext">A <see cref="ChunkGeneratorContext"/> to copy information from.</param>
        /// <param name="errorSink">
        /// The <see cref="ErrorSink"/> used to collect <see cref="RazorError"/>s encountered
        /// when parsing the current Razor document.
        /// </param>
        public CodeGeneratorContext(ChunkGeneratorContext generatorContext, ErrorSink errorSink)
            : base(generatorContext)
        {
            ErrorSink = errorSink;
            ExpressionRenderingMode = ExpressionRenderingMode.WriteToOutput;
        }

        // Internal for testing.
        internal CodeGeneratorContext(
            RazorEngineHost host,
            string className,
            string rootNamespace,
            string sourceFile,
            bool shouldGenerateLinePragmas,
            ErrorSink errorSink)
            : base(host, className, rootNamespace, sourceFile, shouldGenerateLinePragmas)
        {
            ErrorSink = errorSink;
            ExpressionRenderingMode = ExpressionRenderingMode.WriteToOutput;
        }

        /// <summary>
        /// The current C# rendering mode.
        /// </summary>
        /// <remarks>
        /// <see cref="ExpressionRenderingMode.WriteToOutput"/> forces C# generation to write
        /// <see cref="Chunks.Chunk"/>s to the output page, i.e. WriteLiteral("Hello World").
        /// <see cref="ExpressionRenderingMode.InjectCode"/> writes <see cref="Chunks.Chunk"/> values in their
        /// rawest form, i.g. "Hello World".
        /// </remarks>
        public ExpressionRenderingMode ExpressionRenderingMode { get; set; }

        /// <summary>
        /// The C# writer to write <see cref="Chunks.Chunk"/> information to.
        /// </summary>
        /// <remarks>
        /// If <see cref="TargetWriterName"/> is <c>null</c> values will be written using a default write method
        /// i.e. WriteLiteral("Hello World").
        /// If <see cref="TargetWriterName"/> is not <c>null</c> values will be written to the given
        /// <see cref="TargetWriterName"/>, i.e. WriteLiteralTo(myWriter, "Hello World").
        /// </remarks>
        public string TargetWriterName { get; set; }

        /// <summary>
        /// Gets or sets the <c>SHA1</c> based checksum for the file whose location is defined by
        /// <see cref="ChunkGeneratorContext.SourceFile"/>.
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        /// Used to aggregate <see cref="RazorError"/>s.
        /// </summary>
        public ErrorSink ErrorSink { get; }
    }
}
