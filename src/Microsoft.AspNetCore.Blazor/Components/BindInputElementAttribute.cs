// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Components
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class BindInputElementAttribute : Attribute
    {
        public BindInputElementAttribute(string type, string suffix, string valueAttribute, string changeAttribute)
        {
            if (valueAttribute == null)
            {
                throw new ArgumentNullException(nameof(valueAttribute));
            }

            if (changeAttribute == null)
            {
                throw new ArgumentNullException(nameof(changeAttribute));
            }

            Type = type;
            Suffix = suffix;
            ValueAttribute = valueAttribute;
            ChangeAttribute = changeAttribute;
        }
        
        public string Type { get; }
        
        public string Suffix { get; }

        public string ValueAttribute { get; }

        public string ChangeAttribute { get; }
    }
}
