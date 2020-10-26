// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity.UI
{
    /// <summary>
    /// The UIFramework Identity UI will use on the application.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class UIFrameworkAttribute : Attribute
    {

        /// <summary>
        /// Initializes a new instance of <see cref="UIFrameworkAttribute"/>.
        /// </summary>
        /// <param name="uiFramework"></param>
        public UIFrameworkAttribute(string uiFramework)
        {
            UIFramework = uiFramework;
        }

        /// <summary>
        /// The UI Framework Identity UI will use.
        /// </summary>
        public string UIFramework { get; }
    }
}
