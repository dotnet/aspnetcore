// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNet.Routing.Template
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public class TemplatePart
    {
        public static TemplatePart CreateLiteral(string text)
        {
            return new TemplatePart()
            {
                IsLiteral = true,
                Text = text,
            };
        }

        public static TemplatePart CreateParameter(string name, bool isCatchAll, bool isOptional)
        {
            return new TemplatePart()
            {
                IsParameter = true,
                Name = name,
                IsCatchAll = isCatchAll,
                IsOptional = isOptional,
            };
        }

        public bool IsCatchAll { get; private set; }
        public bool IsLiteral { get; private set; }
        public bool IsParameter { get; private set; }
        public bool IsOptional { get; private set; }
        public string Name { get; private set; }
        public string Text { get; private set; }

        internal string DebuggerToString()
        {
            if (IsParameter)
            {
                return "{" + (IsCatchAll ? "*" : string.Empty) + Name + (IsOptional ? "?" : string.Empty) + "}";
            }
            else
            {
                return Text;
            }
        }
    }
}
