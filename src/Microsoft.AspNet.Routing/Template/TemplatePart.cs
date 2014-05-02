// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
