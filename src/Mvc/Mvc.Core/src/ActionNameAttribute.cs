// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Specifies the name of an action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ActionNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new <see cref="ActionNameAttribute"/> instance.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        public ActionNameAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the action.
        /// </summary>
        public string Name { get; }
    }
}