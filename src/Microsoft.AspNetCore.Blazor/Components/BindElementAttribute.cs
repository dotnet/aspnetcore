// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Components
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class BindElementAttribute : Attribute
    {
        public BindElementAttribute(string element, string suffix, string valueAttribute, string changeAttribute)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (valueAttribute == null)
            {
                throw new ArgumentNullException(nameof(valueAttribute));
            }

            if (changeAttribute == null)
            {
                throw new ArgumentNullException(nameof(changeAttribute));
            }

            Element = element;
            ValueAttribute = valueAttribute;
            ChangeAttribute = changeAttribute;
        }
        
        public string Element { get; }

        // Set this to `value` for `bind-value` - set this to null for `bind`
        public string Suffix { get; }

        public string ValueAttribute { get; }

        public string ChangeAttribute { get; }
    }
}
