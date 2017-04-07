// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
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
