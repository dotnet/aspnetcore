// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpLineMappingWriter : IDisposable
    {
        private CSharpCodeWriter _writer;
        private MappingLocation _documentMapping;
        private SourceLocation _generatedLocation;
        private int _startIndent;
        private int _generatedContentLength;
        private bool _writePragmas;
        private bool _addLineMapping;

        private CSharpLineMappingWriter([NotNull] CSharpCodeWriter writer,
                                        bool addLineMappings)
        {
            _writer = writer;
            _addLineMapping = addLineMappings;
            _startIndent = _writer.CurrentIndent;
            _writer.ResetIndent();
        }

        public CSharpLineMappingWriter(CSharpCodeWriter writer, SourceLocation documentLocation, int contentLength)
            : this(writer, addLineMappings: true)
        {
            _documentMapping = new MappingLocation(documentLocation, contentLength);
            _generatedLocation = _writer.GetCurrentSourceLocation();
        }

        public CSharpLineMappingWriter(CSharpCodeWriter writer, SourceLocation documentLocation, int contentLength, string sourceFilename)
            : this(writer, documentLocation, contentLength)
        {
            _writePragmas = true;

            _writer.WriteLineNumberDirective(documentLocation, sourceFilename);
            _generatedLocation = _writer.GetCurrentSourceLocation();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CSharpLineMappingWriter"/> used for generation of runtime
        /// line mappings. The constructed instance of <see cref="CSharpLineMappingWriter"/> does not track
        /// mappings between the Razor content and the generated content.
        /// </summary>
        /// <param name="writer">The <see cref="CSharpCodeWriter"/> to write output to.</param>
        /// <param name="documentLocation">The <see cref="SourceLocation"/> of the Razor content being mapping.</param>
        /// <param name="sourceFileName">The input file path.</param>
        public CSharpLineMappingWriter([NotNull] CSharpCodeWriter writer,
                                       [NotNull] SourceLocation documentLocation,
                                       [NotNull] string sourceFileName)
            : this(writer, addLineMappings: false)
        {
            _writePragmas = true;
            _writer.WriteLineNumberDirective(documentLocation, sourceFileName);
        }

        public void MarkLineMappingStart()
        {
            _generatedLocation = _writer.GetCurrentSourceLocation();
        }

        public void MarkLineMappingEnd()
        {
            _generatedContentLength = _writer.GenerateCode().Length - _generatedLocation.AbsoluteIndex;
        }

        public void Dispose()
        {
            if (_addLineMapping)
            {
                // Verify that the generated length has not already been calculated
                if (_generatedContentLength == 0)
                {
                    _generatedContentLength = _writer.GenerateCode().Length - _generatedLocation.AbsoluteIndex;
                }

                var generatedLocation = new MappingLocation(_generatedLocation, _generatedContentLength);
                if (_documentMapping.ContentLength == -1)
                {
                    _documentMapping.ContentLength = generatedLocation.ContentLength;
                }

                _writer.LineMappingManager.AddMapping(
                    documentLocation: _documentMapping,
                    generatedLocation: new MappingLocation(_generatedLocation, _generatedContentLength));
            }

            if (_writePragmas)
            {
                // Need to add an additional line at the end IF there wasn't one already written.
                // This is needed to work with the C# editor's handling of #line ...
                var endsWithNewline = _writer.GenerateCode().EndsWith("\n");

                // Always write at least 1 empty line to potentially separate code from pragmas.
                _writer.WriteLine();

                // Check if the previous empty line wasn't enough to separate code from pragmas.
                if (!endsWithNewline)
                {
                    _writer.WriteLine();
                }

                _writer.WriteLineDefaultDirective()
                        .WriteLineHiddenDirective();
            }

            // Reset indent back to when it was started
            _writer.SetIndent(_startIndent);
        }
    }
}
