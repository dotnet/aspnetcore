// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

[DebuggerDisplay("{" + nameof(DebuggerToString) + "(),nq}")]
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

    public string GetMessageFormat()
    {
        var message = _messageFormat();
        if (string.IsNullOrEmpty(message))
        {
            return Resources.FormatRazorDiagnosticDescriptor_DefaultError(Id);
        }

        return message;
    }

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

    private string DebuggerToString()
    {
        return $@"Error ""{Id}"": ""{GetMessageFormat()}""";
    }
}
