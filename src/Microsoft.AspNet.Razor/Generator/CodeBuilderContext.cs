// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Generator
{
    /// <summary>
    /// Context object with information used to generate a Razor page.
    /// </summary>
    public class CodeBuilderContext : CodeGeneratorContext
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="CodeBuilderContext"/> object.
        /// </summary>
        /// <param name="generatorContext">A <see cref="CodeGeneratorContext"/> to copy information from.</param>
        public CodeBuilderContext(CodeGeneratorContext generatorContext)
            : base(generatorContext)
        {
            ExpressionRenderingMode = ExpressionRenderingMode.WriteToOutput;
        }

        // Internal for testing.
        internal CodeBuilderContext(RazorEngineHost host,
                                    string className,
                                    string rootNamespace,
                                    string sourceFile,
                                    bool shouldGenerateLinePragmas)
            : base(host, className, rootNamespace, sourceFile, shouldGenerateLinePragmas)
        {
            ExpressionRenderingMode = ExpressionRenderingMode.WriteToOutput;
        }

        /// <summary>
        /// The current C# rendering mode.
        /// </summary>
        /// <remarks>
        /// <see cref="ExpressionRenderingMode.WriteToOutput"/> forces C# generation to write 
        /// <see cref="Compiler.Chunk"/>s to the output page, i.e. WriteLiteral("Hello World").
        /// <see cref="ExpressionRenderingMode.InjectCode"/> writes <see cref="Compiler.Chunk"/> values in their
        /// rawest form, i.g. "Hello World".
        /// </remarks>
        public ExpressionRenderingMode ExpressionRenderingMode { get; set; }

        /// <summary>
        /// The C# writer to write <see cref="Compiler.Chunk"/> information to.
        /// </summary>
        /// <remarks>
        /// If <see cref="TargetWriterName"/> is <c>null</c> values will be written using a default write method
        /// i.e. WriteLiteral("Hello World").
        /// If <see cref="TargetWriterName"/> is not <c>null</c> values will be written to the given 
        /// <see cref="TargetWriterName"/>, i.e. WriteLiteralTo(myWriter, "Hello World").
        /// </remarks>
        public string TargetWriterName { get; set; }

        /// <summary>
        /// Gets or sets the <c>SHA1</c> based checksum for the file whose location is defined by <see cref="SourceFile"/>.
        /// </summary>
        public string Checksum { get; set; }
    }
}
