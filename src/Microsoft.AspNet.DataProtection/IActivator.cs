// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.DataProtection
{
    /// <summary>
    /// An interface into <see cref="Activator.CreateInstance{T}"/> that also supports
    /// limited dependency injection (of <see cref="IServiceProvider"/>).
    /// </summary>
    internal interface IActivator
    {
        /// <summary>
        /// Creates an instance of <paramref name="implementationTypeName"/> and ensures
        /// that it is assignable to <paramref name="expectedBaseType"/>.
        /// </summary>
        object CreateInstance(Type expectedBaseType, string implementationTypeName);
    }
}
