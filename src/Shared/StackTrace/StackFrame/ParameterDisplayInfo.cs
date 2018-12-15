// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.Extensions.StackTrace.Sources
{
    internal class ParameterDisplayInfo
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string Prefix { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(Prefix))
            {
                builder
                    .Append(Prefix)
                    .Append(" ");
            }

            builder.Append(Type);
            builder.Append(" ");
            builder.Append(Name);

            return builder.ToString();
        }
    }
}
