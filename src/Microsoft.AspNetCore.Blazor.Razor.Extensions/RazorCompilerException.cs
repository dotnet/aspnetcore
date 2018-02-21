// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using System;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// Represents a fatal error during the transformation of a Blazor component from
    /// Razor source code to C# source code.
    /// </summary>
    public class RazorCompilerException : Exception
    {
        private readonly int _line;
        private readonly int _column;

        public RazorCompilerException(string message) : this(message, null)
        {
        }

        public RazorCompilerException(string message, SourceSpan? source) : base(message)
        {
            _line = source.HasValue ? (source.Value.LineIndex + 1) : 1;
            _column = source.HasValue ? (source.Value.CharacterIndex + 1) : 1;
        }

        public RazorCompilerDiagnostic ToDiagnostic(string sourceFilePath)
            => new RazorCompilerDiagnostic(
                RazorCompilerDiagnostic.DiagnosticType.Error,
                sourceFilePath,
                line: _line,
                column: _column,
                message: Message);
    }
}
