// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Xml
{
    /// <summary>
    /// Wraps the object of type <see cref="Microsoft.AspNet.Mvc.SerializableError"/>.
    /// </summary>
    public class SerializableErrorWrapperProvider : IWrapperProvider
    {
        /// <inheritdoc />
        public Type WrappingType
        {
            get
            {
                return typeof(SerializableErrorWrapper);
            }
        }

        /// <inheritdoc />
        public object Wrap([NotNull] object original)
        {
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