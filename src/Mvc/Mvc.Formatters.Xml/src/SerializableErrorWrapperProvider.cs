// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    /// <summary>
    /// Wraps the object of type <see cref="Microsoft.AspNetCore.Mvc.SerializableError"/>.
    /// </summary>
    public class SerializableErrorWrapperProvider : IWrapperProvider
    {
        /// <inheritdoc />
        public Type WrappingType => typeof(SerializableErrorWrapper);

        /// <inheritdoc />
        public object Wrap(object original)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            var error = original as SerializableError;
            if (error == null)
            {
                throw new ArgumentException(
                    Resources.FormatWrapperProvider_MismatchType(
                        typeof(SerializableErrorWrapper).Name,
                        original.GetType().Name),
                    nameof(original));
            }

            return new SerializableErrorWrapper(error);
        }
    }
}