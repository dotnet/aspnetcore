// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    /// <summary>
    /// Defines an interface for objects to be un-wrappable after deserialization.
    /// </summary>
    public interface IUnwrappable
    {
        /// <summary>
        /// Unwraps an object.
        /// </summary>
        /// <param name="declaredType">The type to which the object should be un-wrapped to.</param>
        /// <returns>The un-wrapped object.</returns>
        object Unwrap(Type declaredType);
    }
}