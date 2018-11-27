// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    /// <summary>
    /// Defines an interface for wrapping objects for serialization or deserialization into xml.
    /// </summary>
    public interface IWrapperProvider
    {
        /// <summary>
        /// Gets the wrapping type.
        /// </summary>
        Type WrappingType { get; }

        /// <summary>
        /// Wraps the given object to the wrapping type provided by <see cref="WrappingType"/>.
        /// </summary>
        /// <param name="original">The original non-wrapped object.</param>
        /// <returns>Returns a wrapped object.</returns>
        object Wrap(object original);
    }
}