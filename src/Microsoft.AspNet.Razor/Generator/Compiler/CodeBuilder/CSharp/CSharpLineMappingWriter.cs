using System;
using System.Linq;
using Microsoft.AspNet.Razor.Text;

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

        public CSharpLineMappingWriter(CSharpCodeWriter writer, SourceLocation documentLocation, int contentLength)
        {
            _writer = writer;
            _documentMapping = new MappingLocation(documentLocation, contentLength);

            _startIndent = _writer.CurrentIndent;
            _generatedContentLength = 0;
            _writer.ResetIndent();
            
            _generatedLocation = _writer.GetCurrentSourceLocation();
        }

        public CSharpLineMappingWriter(CSharpCodeWriter writer, SourceLocation documentLocation, int contentLength, string sourceFilename)
            : this(writer, documentLocation, contentLength)
        {
            _writePragmas = true;

            // TODO: Should this just be '\n'?
            if (_writer.LastWrite.Last() != '\n')
            {
                _writer.WriteLine();
            }

            _writer.WriteLineNumberDirective(documentLocation.LineIndex + 1, sourceFilename);

            _generatedLocation = _writer.GetCurrentSourceLocation();
        }

        public void MarkLineMappingStart()
        {
            _generatedLocation = _writer.GetCurrentSourceLocation();
        }

        public void MarkLineMappingEnd()
        {
            _generatedContentLength = _writer.ToString().Length - _generatedLocation.AbsoluteIndex;
        }

        public void Dispose()
        {
            // Verify that the generated length has not already been calculated
            if (_generatedContentLength == 0)
            {
                _generatedContentLength = _writer.ToString().Length - _generatedLocation.AbsoluteIndex;
            }

            var generatedLocation = new MappingLocation(_generatedLocation, _generatedContentLength);
            if (_documentMapping.ContentLength == -1)
            {
                _documentMapping.ContentLength = generatedLocation.ContentLength;
            }

            _writer.LineMappingManager.AddMapping(
                documentLocation: _documentMapping,
                generatedLocation: new MappingLocation(_generatedLocation, _generatedContentLength));

            if (_writePragmas)
            {
                // Need to add an additional line at the end IF there wasn't one already written.
                // This is needed to work with the C# editor's handling of #line ...
                bool writeExtraLine = _writer.ToString().Last() != '\n';

                // Always write at least 1 empty line to potentially separate code from pragmas.
                _writer.WriteLine();

                // Check if the previous empty line wasn't enough to separate code from pragmas.
                if (writeExtraLine)
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
