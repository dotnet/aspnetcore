// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.StackTrace.Sources
{
    internal class MethodDisplayInfo
    {
        public string DeclaringTypeName { get; set; }

        public string Name { get; set; }

        public string GenericArguments { get; set; }

        public string SubMethod { get; set; }

        public IEnumerable<ParameterDisplayInfo> Parameters { get; set; }

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
