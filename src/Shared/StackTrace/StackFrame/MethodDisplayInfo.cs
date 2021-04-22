// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

#nullable enable

namespace Microsoft.Extensions.StackTrace.Sources
{
    internal class MethodDisplayInfo
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
                    .Append(".");
            }

            builder.Append(Name);
            builder.Append(GenericArguments);

            builder.Append("(");
            builder.AppendJoin(", ", Parameters.Select(p => p.ToString()));
            builder.Append(")");

            if (!string.IsNullOrEmpty(SubMethod))
            {
                builder.Append("+");
                builder.Append(SubMethod);
                builder.Append("()");
            }

            return builder.ToString();
        }
    }
}
