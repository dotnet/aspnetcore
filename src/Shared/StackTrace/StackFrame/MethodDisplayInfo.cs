// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;

#nullable enable

namespace Microsoft.Extensions.StackTrace.Sources;

internal sealed class MethodDisplayInfo
{
    public MethodDisplayInfo(string? declaringTypeName, string name, string? genericArguments, string? subMethod, IEnumerable<ParameterDisplayInfo> parameters)
    {
        DeclaringTypeName = declaringTypeName;
        Name = name;
        GenericArguments = genericArguments;
        SubMethod = subMethod;
        Parameters = parameters;
    }

    public string? DeclaringTypeName { get; }

    public string Name { get; }

    public string? GenericArguments { get; }

    public string? SubMethod { get; }

    public IEnumerable<ParameterDisplayInfo> Parameters { get; }

    public override string ToString()
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrEmpty(DeclaringTypeName))
        {
            builder
                .Append(DeclaringTypeName)
                .Append('.');
        }

        builder.Append(Name);
        builder.Append(GenericArguments);

        builder.Append('(');
        builder.AppendJoin(", ", Parameters.Select(p => p.ToString()));
        builder.Append(')');

        if (!string.IsNullOrEmpty(SubMethod))
        {
            builder.Append('+');
            builder.Append(SubMethod);
            builder.Append("()");
        }

        return builder.ToString();
    }
}
