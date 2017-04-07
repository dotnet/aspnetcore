// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorDiagnostic : IEquatable<RazorDiagnostic>, IFormattable
    {
        internal static readonly object[] EmptyArgs = new object[0];

        public abstract string Id { get; }

        public abstract RazorDiagnosticSeverity Severity { get; }

        public abstract SourceSpan Span { get; }

        public abstract string GetMessage(IFormatProvider formatProvider);

        public string GetMessage() => GetMessage(null);

        public abstract bool Equals(RazorDiagnostic other);

        public override abstract int GetHashCode();

        public static RazorDiagnostic Create(RazorDiagnosticDescriptor descriptor, SourceSpan span)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new DefaultRazorDiagnostic(descriptor, span, EmptyArgs);
        }

        public static RazorDiagnostic Create(RazorDiagnosticDescriptor descriptor, SourceSpan span, params object[] args)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new DefaultRazorDiagnostic(descriptor, span, args);
        }

        internal static RazorDiagnostic Create(RazorError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            return new LegacyRazorDiagnostic(error);
        }

        public override string ToString()
        {
            return ((IFormattable)this).ToString(null, null);
        }

        public override bool Equals(object obj)
        {
            var other = obj as RazorDiagnostic;
            return other == null ? false : Equals(other);
        }

        string IFormattable.ToString(string ignore, IFormatProvider formatProvider)
        {
            // Our indices are 0-based, but we we want to print them as 1-based.
            return $"{Span.FilePath}({Span.LineIndex + 1},{Span.CharacterIndex + 1}): {Severity} {Id}: {GetMessage(formatProvider)}";
        }
    }
}
