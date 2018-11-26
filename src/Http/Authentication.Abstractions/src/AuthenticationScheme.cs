// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// AuthenticationSchemes assign a name to a specific <see cref="IAuthenticationHandler"/>
    /// handlerType.
    /// </summary>
    public class AuthenticationScheme
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name for the authentication scheme.</param>
        /// <param name="displayName">The display name for the authentication scheme.</param>
        /// <param name="handlerType">The <see cref="IAuthenticationHandler"/> type that handles this scheme.</param>
        public AuthenticationScheme(string name, string displayName, Type handlerType)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (handlerType == null)
            {
                throw new ArgumentNullException(nameof(handlerType));
            }
            if (!typeof(IAuthenticationHandler).IsAssignableFrom(handlerType))
            {
                throw new ArgumentException("handlerType must implement IAuthenticationHandler.");
            }

            Name = name;
            HandlerType = handlerType;
            DisplayName = displayName;
        }

        /// <summary>
        /// The name of the authentication scheme.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The display name for the scheme. Null is valid and used for non user facing schemes.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The <see cref="IAuthenticationHandler"/> type that handles this scheme.
        /// </summary>
        public Type HandlerType { get; }
    }
}
