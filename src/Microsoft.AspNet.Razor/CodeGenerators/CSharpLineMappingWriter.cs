// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.CodeGenerators
{
    public class CSharpLineMappingWriter : IDisposable
    {
        private readonly CSharpCodeWriter _writer;
        private readonly MappingLocation _documentMapping;
        private readonly int _startIndent;
        private readonly bool _writePragmas;
        private readonly bool _addLineMapping;

        private SourceLocation _generatedLocation;
        private int _generatedContentLength;

        private CSharpLineMappingWriter(CSharpCodeWriter writer, bool addLineMappings)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

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

        public CSharpLineMappingWriter(
            CSharpCodeWriter writer,
            SourceLocation documentLocation,
            int contentLength,
            string sourceFilename)
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
        public CSharpLineMappingWriter(
            CSharpCodeWriter writer,
            SourceLocation documentLocation,
            string sourceFileName)
            : this(writer, addLineMappings: false)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

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
                var documentMapping = _documentMapping;
                if (documentMapping.ContentLength == -1)
                {
                    documentMapping = new MappingLocation(
                        location: new SourceLocation(
                            _documentMapping.AbsoluteIndex,
                            _documentMapping.LineIndex,
                            _documentMapping.CharacterIndex),
                        contentLength: _generatedContentLength);
                }

                _writer.LineMappingManager.AddMapping(
                    documentLocation: documentMapping,
                    generatedLocation: generatedLocation);
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
