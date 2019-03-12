// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Customizes the name of a hub method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class HubMethodNameAttribute : Attribute
    {
        /// <summary>
        /// The customized name of the hub method.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="HubMethodNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The customized name of the hub method.</param>
        public HubMethodNameAttribute(string name)
        {
            Name = name;
        }
    }
}
