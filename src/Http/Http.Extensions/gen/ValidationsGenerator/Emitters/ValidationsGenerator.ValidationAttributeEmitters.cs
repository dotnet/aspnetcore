// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public partial class ValidationsGenerator
{
    internal static string EmitValidationAttribute(ValidationAttribute attribute)
    {
        var builder = new StringBuilder();
        if (attribute.ForParameter)
        {
            builder.Append("var ");
        }
        else
        {
            builder.Append("private static readonly ValidationAttribute ");
        }
        builder.Append(attribute.Name);
        builder.Append(' ');
        builder.Append("= ");
        builder.Append("new ");
        builder.Append(attribute.ClassName);
        builder.Append('(');
        for (var i = 0; i < attribute.Arguments.Count; i++)
        {
            builder.Append(attribute.Arguments[i]);
            if (i < attribute.Arguments.Count - 1)
            {
                builder.Append(", ");
            }
        }
        if (attribute.NamedArguments.Count > 0)
        {
            builder.Append(") { ");
            foreach (var kvp in attribute.NamedArguments)
            {
                builder.Append(kvp.Key);
                builder.Append(" = ");
                builder.Append(kvp.Value);
            }
            builder.Append(" };");
        }
        else
        {
            builder.Append(");");
        }
        return builder.ToString();
    }
}
