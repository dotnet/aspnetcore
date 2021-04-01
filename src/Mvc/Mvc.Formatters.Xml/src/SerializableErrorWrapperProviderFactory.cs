// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    /// <summary>
    /// Creates an <see cref="IWrapperProvider"/> for the type <see cref="Microsoft.AspNetCore.Mvc.SerializableError"/>.
    /// </summary>
    public class SerializableErrorWrapperProviderFactory : IWrapperProviderFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="SerializableErrorWrapperProvider"/> if the provided
        /// <paramref name="context"/>'s <see cref="WrapperProviderContext.DeclaredType"/> is
        /// <see cref="Microsoft.AspNetCore.Mvc.SerializableError"/>.
        /// </summary>
        /// <param name="context">The <see cref="WrapperProviderContext"/>.</param>
        /// <returns>
        /// An instance of <see cref="SerializableErrorWrapperProvider"/> if the provided <paramref name="context"/>'s
        /// <see cref="WrapperProviderContext.DeclaredType"/> is
        /// <see cref="Microsoft.AspNetCore.Mvc.SerializableError"/>; otherwise <c>null</c>.
        /// </returns>
        public IWrapperProvider GetProvider(WrapperProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.DeclaredType == typeof(SerializableError))
            {
                return new SerializableErrorWrapperProvider();
            }

            return null;
        }
    }
}