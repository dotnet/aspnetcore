// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    /// <summary>
    /// The context used by an <see cref="IWrapperProvider"/> to wrap or un-wrap types.
    /// </summary>
    public class WrapperProviderContext
    {
        /// <summary>
        /// Initializes a <see cref="WrapperProviderContext"/>.
        /// </summary>
        /// <param name="declaredType">The declared type of the object that needs to be wrapped.</param>
        /// <param name="isSerialization"><see langword="true"/> if the wrapper provider is invoked during
        /// serialization, otherwise <see langword="false"/>.</param>
        public WrapperProviderContext(Type declaredType, bool isSerialization)
        {
            if (declaredType == null)
            {
                throw new ArgumentNullException(nameof(declaredType));
            }

            DeclaredType = declaredType;
            IsSerialization = isSerialization;
        }

        /// <summary>
        /// The declared type which could be wrapped/un-wrapped by a different type 
        /// during serialization or deserialization.
        /// </summary>
        public Type DeclaredType { get; }

        /// <summary>
        /// <see langword="true"/> if a wrapper provider is invoked during serialization,
        /// <see langword="false"/> otherwise.
        /// </summary>
        public bool IsSerialization { get; }
    }
}