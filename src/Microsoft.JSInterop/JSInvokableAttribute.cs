// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Identifies a .NET method as allowing invocation from JavaScript code.
    /// Any method marked with this attribute may receive arbitrary parameter values
    /// from untrusted callers. All inputs should be validated carefully.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class JSInvokableAttribute : Attribute
    {
        /// <summary>
        /// Gets the identifier for the method. The identifier must be unique within the scope
        /// of an assembly.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Constructs an instance of <see cref="JSInvokableAttribute"/>.
        /// </summary>
        /// <param name="identifier">An identifier for the method, which must be unique within the scope of the assembly.</param>
        public JSInvokableAttribute(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(identifier));
            }

            Identifier = identifier;
        }
    }
}
