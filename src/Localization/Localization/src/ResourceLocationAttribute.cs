// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Localization
{
    /// <summary>
    /// Provides the location of resources for an Assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ResourceLocationAttribute : Attribute
    {
        /// <summary>
        /// Creates a new <see cref="ResourceLocationAttribute"/>.
        /// </summary>
        /// <param name="resourceLocation">The location of resources for this Assembly.</param>
        public ResourceLocationAttribute(string resourceLocation)
        {
            if (string.IsNullOrEmpty(resourceLocation))
            {
                throw new ArgumentNullException(nameof(resourceLocation));
            }

            ResourceLocation = resourceLocation;
        }

        /// <summary>
        /// The location of resources for this Assembly.
        /// </summary>
        public string ResourceLocation { get; }
    }
}
