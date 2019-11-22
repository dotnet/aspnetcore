// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    [Obsolete("This attribute has been superseded by RazorCompiledItem and will not be used by the runtime.")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RazorViewAttribute : Attribute
    {
        public RazorViewAttribute(string path, Type viewType)
        {
            Path = path;
            ViewType = viewType;
        }

        /// <summary>
        /// Gets the path of the view.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the view type.
        /// </summary>
        public Type ViewType { get; }
    }
}
