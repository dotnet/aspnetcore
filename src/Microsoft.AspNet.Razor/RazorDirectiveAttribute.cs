// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            var attribute = obj as RazorDirectiveAttribute;
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
