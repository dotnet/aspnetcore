// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    [DebuggerDisplay("Error {Id}: {GetMessageFormat()}")]
    public sealed class RazorDiagnosticDescriptor : IEquatable<RazorDiagnosticDescriptor>
    {
        private readonly Func<string> _messageFormat;

        public RazorDiagnosticDescriptor(
            string id,
            Func<string> messageFormat, 
            RazorDiagnosticSeverity severity)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(id));
            }

            if (messageFormat == null)
            {
                throw new ArgumentNullException(nameof(messageFormat));
            }

            Id = id;
            _messageFormat = messageFormat;
            Severity = severity;
        }

        public string Id { get; }

        public RazorDiagnosticSeverity Severity { get; }

        public string GetMessageFormat() => _messageFormat();

        public override bool Equals(object obj)
        {
            return Equals(obj as RazorDiagnosticDescriptor);
        }

        public bool Equals(RazorDiagnosticDescriptor other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(Id, other.Id, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Id);
        }
    }
}
