using System;
using System.IO;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeWriter : IDisposable
    {
        protected StringWriter Writer;

        private bool _newLine;
        private string _cache;
        private bool _dirty;

        public CodeWriter()
        {
            Writer = new StringWriter();
            _dirty = true;
        }

        public string LastWrite { get; private set; }
        public int CurrentIndent { get; private set; }

        public CodeWriter ResetIndent()
        {
            return SetIndent(0);
        }

        public CodeWriter IncreaseIndent(int size)
        {
            CurrentIndent += size;

            return this;
        }

        public CodeWriter DecreaseIndent(int size)
        {
            CurrentIndent -= size;

            return this;
        }

        public CodeWriter SetIndent(int size)
        {
            CurrentIndent = size;

            return this;
        }

        public CodeWriter Indent(int size)
        {
            if (_newLine)
            {
                Writer.Write(new string(' ', size));
                Flush();
                _dirty = true;
                _newLine = false;
            }

            return this;
        }

        public CodeWriter Write(string data)
        {
            Indent(CurrentIndent);

            Writer.Write(data);

            Flush();

            LastWrite = data;
            _dirty = true;
            _newLine = false;

            return this;
        }

        public CodeWriter WriteLine()
        {
            LastWrite = Environment.NewLine;

            Writer.WriteLine();

            Flush();

            _dirty = true;
            _newLine = true;

            return this;
        }

        public CodeWriter WriteLine(string data)
        {
            return Write(data).WriteLine();
        }

        public CodeWriter Flush()
        {
            Writer.Flush();

            return this;
        }

        public override string ToString()
        {
            if (_dirty)
            {
                _cache = Writer.ToString();
            }

            return _cache;
        }

        public SourceLocation GetCurrentSourceLocation()
        {
            string output = ToString();
            string unescapedOutput = output.Replace("\\r", String.Empty).Replace("\\n", String.Empty);

            return new SourceLocation(
                absoluteIndex: output.Length,
                lineIndex: (unescapedOutput.Length - unescapedOutput.Replace(Environment.NewLine, String.Empty).Length) / Environment.NewLine.Length,
                characterIndex: unescapedOutput.Length - (unescapedOutput.LastIndexOf(Environment.NewLine) + Environment.NewLine.Length));
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                Writer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
        }
    }
}
