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

using System;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor
{
    /// <summary>
    /// Specifies a Razor directive that is rendered as an attribute on the generated class. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class RazorDirectiveAttribute : Attribute
    {
        public RazorDirectiveAttribute(string name, string value)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "name");
            }

            Name = name;
            Value = value;
        }

        public string Name { get; private set; }

        public string Value { get; private set; }

        public override bool Equals(object obj)
        {
            RazorDirectiveAttribute attribute = obj as RazorDirectiveAttribute;
            return attribute != null &&
                   Name.Equals(attribute.Name, StringComparison.OrdinalIgnoreCase) &&
                   StringComparer.OrdinalIgnoreCase.Equals(Value, attribute.Value);
        }

        public override int GetHashCode()
        {
            return (StringComparer.OrdinalIgnoreCase.GetHashCode(Name) * 31) +
                   (Value == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value));
        }
    }
}
