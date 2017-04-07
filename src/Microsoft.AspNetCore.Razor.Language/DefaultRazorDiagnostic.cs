// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorDiagnostic : RazorDiagnostic
    {
        private readonly RazorDiagnosticDescriptor _descriptor;
        private readonly object[] _args;

        internal DefaultRazorDiagnostic(RazorDiagnosticDescriptor descriptor, SourceSpan span, object[] args)
        {
            _descriptor = descriptor;
            Span = span;
            _args = args;
        }

        public override string Id => _descriptor.Id;

        public override RazorDiagnosticSeverity Severity => _descriptor.Severity;

        public override SourceSpan Span { get; }

        public override string GetMessage(IFormatProvider formatProvider)
        {
            var format = _descriptor.GetMessageFormat();
            return string.Format(formatProvider, format, _args);
        }

        public override bool Equals(RazorDiagnostic obj)
        {
            var other = obj as DefaultRazorDiagnostic;
            if (other == null)
            {
                return false;
            }

            if (!_descriptor.Equals(other._descriptor))
            {
                return false;
            }
            
            if (!Span.Equals(other.Span))
            {
                return false;
            }

            if (_args.Length != other._args.Length)
            {
                return false;
            }

            for (var i = 0; i < _args.Length; i++)
            {
                if (!_args[i].Equals(other._args[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hash = new HashCodeCombiner();
            hash.Add(_descriptor.GetHashCode());
            hash.Add(Span.GetHashCode());

            for (var i = 0; i < _args.Length; i++)
            {
                hash.Add(_args[i]);
            }

            return hash;
        }
    }
}
