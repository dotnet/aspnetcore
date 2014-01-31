using System;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public struct CSharpDisableWarningScope : IDisposable
    {
        private CSharpCodeWriter _writer;
        int _warningNumber;

        public CSharpDisableWarningScope(CSharpCodeWriter writer) : this(writer, 219)
        { }

        public CSharpDisableWarningScope(CSharpCodeWriter writer, int warningNumber)
        {
            _writer = writer;
            _warningNumber = warningNumber;

            _writer.WritePragma("warning disable " + _warningNumber);
        }

        public void Dispose()
        {
            _writer.WritePragma("warning restore " + _warningNumber);
        }
    }
}
