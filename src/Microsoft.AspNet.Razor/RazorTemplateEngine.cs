// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor
{
    /// <summary>
    /// Entry-point to the Razor Template Engine
    /// </summary>
    public class RazorTemplateEngine
    {
        private const int BufferSize = 1024;
        public static readonly string DefaultClassName = "Template";
        public static readonly string DefaultNamespace = String.Empty;

        /// <summary>
        /// Constructs a new RazorTemplateEngine with the specified host
        /// </summary>
        /// <param name="host">The host which defines the environment in which the generated template code will live</param>
        public RazorTemplateEngine(RazorEngineHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException("host");
            }

            Host = host;
        }

        /// <summary>
        /// The RazorEngineHost which defines the environment in which the generated template code will live
        /// </summary>
        public RazorEngineHost Host { get; private set; }

        public ParserResults ParseTemplate(ITextBuffer input)
        {
            return ParseTemplate(input, null);
        }

        /// <summary>
        /// Parses the template specified by the TextBuffer and returns it's result
        /// </summary>
        /// <remarks>
        /// IMPORTANT: This does NOT need to be called before GeneratedCode! GenerateCode will automatically
        /// parse the document first.
        /// 
        /// The cancel token provided can be used to cancel the parse.  However, please note
        /// that the parse occurs _synchronously_, on the callers thread.  This parameter is 
        /// provided so that if the caller is in a background thread with a CancellationToken, 
        /// it can pass it along to the parser.
        /// </remarks>
        /// <param name="input">The input text to parse</param>
        /// <param name="cancelToken">A token used to cancel the parser</param>
        /// <returns>The resulting parse tree</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Input object would be disposed if we dispose the wrapper.  We don't own the input so we don't want to dispose it")]
        public ParserResults ParseTemplate(ITextBuffer input, CancellationToken? cancelToken)
        {
            return ParseTemplateCore(input.ToDocument(), cancelToken);
        }

        // See ParseTemplate(ITextBuffer, CancellationToken?), 
        // this overload simply wraps a TextReader in a TextBuffer (see ITextBuffer and BufferingTextReader)
        public ParserResults ParseTemplate(TextReader input)
        {
            return ParseTemplate(input, null);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Input object would be disposed if we dispose the wrapper.  We don't own the input so we don't want to dispose it")]
        public ParserResults ParseTemplate(TextReader input, CancellationToken? cancelToken)
        {
            return ParseTemplateCore(new SeekableTextReader(input), cancelToken);
        }

        protected internal virtual ParserResults ParseTemplateCore(ITextDocument input, CancellationToken? cancelToken)
        {
            // Construct the parser
            RazorParser parser = CreateParser();
            Debug.Assert(parser != null);
            return parser.Parse(input);
        }

        public GeneratorResults GenerateCode(ITextBuffer input)
        {
            return GenerateCode(input, null, null, null, null);
        }

        public GeneratorResults GenerateCode(ITextBuffer input, CancellationToken? cancelToken)
        {
            return GenerateCode(input, null, null, null, cancelToken);
        }

        public GeneratorResults GenerateCode(ITextBuffer input, string className, string rootNamespace, string sourceFileName)
        {
            return GenerateCode(input, className, rootNamespace, sourceFileName, null);
        }

        /// <summary>
        /// Parses the template specified by the TextBuffer, generates code for it, and returns the constructed code.
        /// </summary>
        /// <remarks>
        /// The cancel token provided can be used to cancel the parse.  However, please note
        /// that the parse occurs _synchronously_, on the callers thread.  This parameter is 
        /// provided so that if the caller is in a background thread with a CancellationToken, 
        /// it can pass it along to the parser.
        /// 
        /// The className, rootNamespace and sourceFileName parameters are optional and override the default
        /// specified by the Host.  For example, the WebPageRazorHost in System.Web.WebPages.Razor configures the
        /// Class Name, Root Namespace and Source File Name based on the virtual path of the page being compiled.
        /// However, the built-in RazorEngineHost class uses constant defaults, so the caller will likely want to 
        /// change them using these parameters
        /// </remarks>
        /// <param name="input">The input text to parse</param>
        /// <param name="cancelToken">A token used to cancel the parser</param>
        /// <param name="className">The name of the generated class, overriding whatever is specified in the Host.  The default value (defined in the Host) can be used by providing null for this argument</param>
        /// <param name="rootNamespace">The namespace in which the generated class will reside, overriding whatever is specified in the Host.  The default value (defined in the Host) can be used by providing null for this argument</param>
        /// <param name="sourceFileName">The file name to use in line pragmas, usually the original Razor file, overriding whatever is specified in the Host.  The default value (defined in the Host) can be used by providing null for this argument</param>
        /// <returns>The resulting parse tree AND generated code.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Input object would be disposed if we dispose the wrapper.  We don't own the input so we don't want to dispose it")]
        public GeneratorResults GenerateCode(ITextBuffer input, string className, string rootNamespace, string sourceFileName, CancellationToken? cancelToken)
        {
            return GenerateCodeCore(input.ToDocument(),
                                    className,
                                    rootNamespace,
                                    sourceFileName,
                                    checksum: null,
                                    cancelToken: cancelToken);
        }

        // See GenerateCode override which takes ITextBuffer, and BufferingTextReader for details.
        public GeneratorResults GenerateCode(TextReader input)
        {
            return GenerateCode(input, null, null, null, null);
        }

        public GeneratorResults GenerateCode(TextReader input, CancellationToken? cancelToken)
        {
            return GenerateCode(input, null, null, null, cancelToken);
        }

        public GeneratorResults GenerateCode(TextReader input, string className, string rootNamespace, string sourceFileName)
        {
            return GenerateCode(input, className, rootNamespace, sourceFileName, null);
        }

        /// <summary>
        /// Parses the contents specified by the <paramref name="inputStream"/> and returns the generated code.
        /// </summary>
        /// <param name="inputStream">A <see cref="Stream"/> that represents the contents to be parsed.</param>
        /// <param name="className">The name of the generated class. When <c>null</c>, defaults to
        /// <see cref="Host.DefaultClassName"/>.</param>
        /// <param name="rootNamespace">The namespace in which the generated class will reside. When <c>null</c>,
        /// defaults to <see cref="Host.DefaultNamespace"/>.</param>
        /// <param name="sourceFileName">The file name to use in line pragmas, usually the original Razor file.</param>
        /// <returns>A <see cref="GeneratorResults"/> that represents the results of parsing the content.</returns>
        /// <remarks>
        /// This overload calculates the checksum of the contents of <paramref name="inputStream"/> prior to code
        /// generation. The checksum is used for producing the <c>#pragma checksum</c> line pragma required for
        /// debugging.
        /// </remarks>
        public GeneratorResults GenerateCode([NotNull] Stream inputStream,
                                             string className,
                                             string rootNamespace,
                                             string sourceFileName)
        {
            MemoryStream memoryStream = null;
            string checksum = null;
            try
            {
                if (!Host.DesignTimeMode)
                {
                    // We don't need to calculate the checksum in design time.

                    if (!inputStream.CanSeek)
                    {
                        memoryStream = new MemoryStream();
                        inputStream.CopyTo(memoryStream);

                        // We don't have to dispose the input stream since it is owned externally.
                        inputStream = memoryStream;
                    }

                    inputStream.Position = 0;
                    checksum = ComputeChecksum(inputStream);
                    inputStream.Position = 0;
                }

                using (var reader = new StreamReader(inputStream,
                                                     Encoding.UTF8,
                                                     detectEncodingFromByteOrderMarks: true,
                                                     bufferSize: BufferSize,
                                                     leaveOpen: true))
                {
                    var seekableStream = new SeekableTextReader(reader);
                    return GenerateCodeCore(seekableStream,
                                            className,
                                            rootNamespace,
                                            sourceFileName,
                                            checksum,
                                            cancelToken: null);
                }
            }
            finally
            {
                if (memoryStream != null)
                {
                    memoryStream.Dispose();
                }
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Input object would be disposed if we dispose the wrapper.  We don't own the input so we don't want to dispose it")]
        public GeneratorResults GenerateCode(TextReader input, string className, string rootNamespace, string sourceFileName, CancellationToken? cancelToken)
        {
            return GenerateCodeCore(new SeekableTextReader(input),
                                    className,
                                    rootNamespace,
                                    sourceFileName,
                                    checksum: null,
                                    cancelToken: cancelToken);
        }

        protected internal virtual GeneratorResults GenerateCodeCore(ITextDocument input,
                                                                     string className,
                                                                     string rootNamespace,
                                                                     string sourceFileName,
                                                                     string checksum,
                                                                     CancellationToken? cancelToken)
        {
            className = (className ?? Host.DefaultClassName) ?? DefaultClassName;
            rootNamespace = (rootNamespace ?? Host.DefaultNamespace) ?? DefaultNamespace;

            // Run the parser
            RazorParser parser = CreateParser();
            Debug.Assert(parser != null);
            ParserResults results = parser.Parse(input);

            // Generate code
            RazorCodeGenerator generator = CreateCodeGenerator(className, rootNamespace, sourceFileName);
            generator.DesignTimeMode = Host.DesignTimeMode;
            generator.Visit(results);

            var codeBuilderContext = new CodeBuilderContext(generator.Context);
            codeBuilderContext.Checksum = checksum;
            var builder = CreateCodeBuilder(codeBuilderContext);
            var builderResult = builder.Build();

            // Collect results and return
            return new GeneratorResults(results, builderResult);
        }

        protected internal virtual RazorCodeGenerator CreateCodeGenerator(string className, string rootNamespace, string sourceFileName)
        {
            return Host.DecorateCodeGenerator(
                Host.CodeLanguage.CreateCodeGenerator(className, rootNamespace, sourceFileName, Host));
        }

        protected internal virtual RazorParser CreateParser()
        {
            ParserBase codeParser = Host.CodeLanguage.CreateCodeParser();
            ParserBase markupParser = Host.CreateMarkupParser();

            return new RazorParser(Host.DecorateCodeParser(codeParser),
                                   Host.DecorateMarkupParser(markupParser))
            {
                DesignTimeMode = Host.DesignTimeMode
            };
        }

        protected internal virtual CodeBuilder CreateCodeBuilder(CodeBuilderContext context)
        {
            return Host.DecorateCodeBuilder(Host.CodeLanguage.CreateCodeBuilder(context),
                                            context);
        }

        private static string ComputeChecksum(Stream inputStream)
        {
            byte[] hashedBytes;
            using (var hashAlgorithm = SHA1.Create())
            {
                hashedBytes = hashAlgorithm.ComputeHash(inputStream);
            }

            var fileHashBuilder = new StringBuilder(hashedBytes.Length * 2);
            foreach (var value in hashedBytes)
            {
                fileHashBuilder.Append(value.ToString("x2"));
            }
            return fileHashBuilder.ToString();
        }
    }
}
