// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// This attribute specifies the metadata class to associate with a data model class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ModelMetadataTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelMetadataTypeAttribute" /> class.
        /// </summary>
        /// <param name="type">The type of metadata class that is associated with a data model class.</param>
        public ModelMetadataTypeAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            MetadataType = type;
        }

        /// <summary>
        /// Gets the type of metadata class that is associated with a data model class.
        /// </summary>
        public Type MetadataType { get; }
    }
}