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
    /// Emits the <c>ValidatableInfo</c> family (and the runtime parameter resolver) as file-local
    /// classes so they no longer need to ship from the Microsoft.Extensions.Validation assembly.
    /// The classes live in the <c>Microsoft.Extensions.Validation.Generated</c> namespace alongside the
    /// generated resolver, and rely on enclosing-namespace resolution to reference the public
    /// Microsoft.Extensions.Validation surface (ValidateContext, ValidationOptions, DisplayNameInfo, ...).
    /// </summary>
    internal static string EmitInfoClasses() => s_infoClasses.Value;

    private static string BuildInfoClasses()
    {
        var assembly = typeof(ValidationsGenerator).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.Extensions.Validation.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    using System;");
        sb.AppendLine("    using System.Collections;");
        sb.AppendLine("    using System.Collections.Generic;");
        sb.AppendLine("    using System.ComponentModel;");
        sb.AppendLine("    using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("    using System.Diagnostics.CodeAnalysis;");
        sb.AppendLine("    using System.IO;");
        sb.AppendLine("    using System.IO.Pipelines;");
        sb.AppendLine("    using System.Linq;");
        sb.AppendLine("    using System.Reflection;");
        sb.AppendLine("    using System.Security.Claims;");
        sb.AppendLine("    using System.Threading;");
        sb.AppendLine("    using System.Threading.Tasks;");
        sb.AppendLine();

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

        sb.AppendLine("}");
        return sb.ToString();
    }
}
