// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Formatters.Xml
{
    /// <summary>
    /// Creates an <see cref="IWrapperProvider"/> for the type <see cref="Microsoft.AspNet.Mvc.SerializableError"/>.
    /// </summary>
    public class SerializableErrorWrapperProviderFactory : IWrapperProviderFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="SerializableErrorWrapperProvider"/> if the provided
        /// <paramref name="context"/>'s <see cref="WrapperProviderContext.DeclaredType"/> is
        /// <see cref="Microsoft.AspNet.Mvc.SerializableError"/>.
        /// </summary>
        /// <param name="context">The <see cref="WrapperProviderContext"/>.</param>
        /// <returns>
        /// An instance of <see cref="SerializableErrorWrapperProvider"/> if the provided <paramref name="context"/>'s
        /// <see cref="WrapperProviderContext.DeclaredType"/> is
        /// <see cref="Microsoft.AspNet.Mvc.SerializableError"/>; otherwise <c>null</c>.
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