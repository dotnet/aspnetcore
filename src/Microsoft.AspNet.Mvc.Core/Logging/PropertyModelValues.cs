// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of a <see cref="PropertyModelValues"/>. Logged as a substructure of
    /// <see cref="ControllerModelValues"/>, this contains the name, type, and
    /// binder metadata of the property.
    /// </summary>
    public class PropertyModelValues : ReflectionBasedLogValues
    {
        public PropertyModelValues([NotNull] PropertyModel inner)
        {
            PropertyName = inner.PropertyName;
            PropertyType = inner.PropertyInfo.PropertyType;
        }

        /// <summary>
        /// The name of the property. See <see cref="PropertyModel.PropertyName"/>.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// The <see cref="Type"/> of the property.
        /// </summary>
        public Type PropertyType { get; }

        public override string Format()
        {
            return LogFormatter.FormatLogValues(this);
        }
    }
}