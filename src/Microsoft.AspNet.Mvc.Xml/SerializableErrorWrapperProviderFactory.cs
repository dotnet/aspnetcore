// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Xml
{
    /// <summary>
    /// Creates an <see cref="IWrapperProvider"/> for the type <see cref="Microsoft.AspNet.Mvc.SerializableError"/>.
    /// </summary>
    public class SerializableErrorWrapperProviderFactory : IWrapperProviderFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="SerializableErrorWrapperProvider"/> if the provided
        /// declared type is <see cref="Microsoft.AspNet.Mvc.SerializableError"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>An instance of <see cref="SerializableErrorWrapperProvider"/> if the provided 
        /// declared type is <see cref="Microsoft.AspNet.Mvc.SerializableError"/>, else <c>null</c>.</returns>
        public IWrapperProvider GetProvider([NotNull] WrapperProviderContext context)
        {
            if (context.DeclaredType == typeof(SerializableError))
            {
                return new SerializableErrorWrapperProvider();
            }

            return null;
        }
    }
}