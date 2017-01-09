// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class LegacyRazorDiagnostic : RazorDiagnostic
    {
        private readonly RazorError _error;

        public LegacyRazorDiagnostic(RazorError error)
        {
            _error = error;
        }

        public override string Id => "RZ9999";

        public override RazorDiagnosticSeverity Severity => RazorDiagnosticSeverity.Error;

        public override SourceSpan Span => new SourceSpan(_error.Location, _error.Length);

        public override string GetMessage(IFormatProvider formatProvider)
        {
            return _error.Message;
        }

        public override bool Equals(RazorDiagnostic obj)
        {
            var other = obj as LegacyRazorDiagnostic;
            return other == null ? false : _error.Equals(other._error);
        }

        public override int GetHashCode()
        {
            return _error.GetHashCode();
        }
    }
}
