// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Build.Core.RazorCompilation
{
    /// <summary>
    /// Represents a fatal error during the transformation of a Blazor component from
    /// Razor source code to C# source code.
    /// </summary>
    internal class RazorCompilerException : Exception
    {
        public RazorCompilerException(string message): base(message)
        {
        }

        public RazorCompilerDiagnostic ToDiagnostic(string sourceFilePath)
            => new RazorCompilerDiagnostic(
                RazorCompilerDiagnostic.DiagnosticType.Error,
                sourceFilePath,
                line: 1, // Later it might be necessary to take line/col constructor args, but not needed yet
                column: 1,
                message: Message);
    }
}
