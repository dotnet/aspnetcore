// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;

namespace Microsoft.Extensions.Validation;

public sealed partial class ValidationsGenerator
{
    // Emitted in this order so base classes precede derived ones. The order is not required by the
    // C# compiler, but keeps the generated output readable.
    private static readonly string[] s_infoClassTemplates =
    [
        "DisplayNameInfo.cs",
        "ValidatableInfo.cs",
        "ValidatableTypeInfo.cs",
        "ValidatablePropertyInfo.cs",
        "ValidatableParameterInfo.cs",
        "RuntimeValidatableParameterInfoResolver.cs",
    ];

    private static readonly Lazy<string> s_infoClasses = new(BuildInfoClasses);

    /// <summary>
    /// Emits the <c>DisplayNameInfo</c> and <c>ValidatableInfo</c> family (and the runtime parameter
    /// resolver) as file-local class bodies so they no longer need to ship from the
    /// Microsoft.Extensions.Validation assembly. The bodies are emitted directly inside the existing
    /// <c>Microsoft.Extensions.Validation.Generated</c> namespace block, alongside the generated
    /// resolver. Every type reference is fully qualified with <c>global::</c> so the emitted code needs
    /// no <c>using</c> directives.
    /// </summary>
    internal static string EmitInfoClasses() => s_infoClasses.Value;

    private static string BuildInfoClasses()
    {
        var assembly = typeof(ValidationsGenerator).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        var sb = new StringBuilder();

        foreach (var template in s_infoClassTemplates)
        {
            var resourceName = Array.Find(resourceNames, n => n.EndsWith("." + template, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"Embedded template '{template}' was not found in the generator assembly.");

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var body = reader.ReadToEnd();

            foreach (var line in body.Replace("\r\n", "\n").Split('\n'))
            {
                if (line.Length == 0)
                {
                    sb.AppendLine();
                }
                else
                {
                    sb.Append("    ").AppendLine(line);
                }
            }

            sb.AppendLine();
        }

        // Trim the trailing newline so the emitted classes sit flush before the enclosing
        // namespace's closing brace in the main template.
        return sb.ToString().TrimEnd('\r', '\n');
    }
}
