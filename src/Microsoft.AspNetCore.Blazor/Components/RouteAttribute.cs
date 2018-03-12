// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Components
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RouteAttribute : Attribute
    {
        public RouteAttribute(string template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            Template = template;
        }

        public string Template { get; }
    }
}
