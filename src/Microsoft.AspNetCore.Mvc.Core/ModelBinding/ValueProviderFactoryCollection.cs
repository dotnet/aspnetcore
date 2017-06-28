// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Represents a collection of value provider factories.
    /// </summary>
    public class ValueProviderFactoryCollection : Collection<IValueProviderFactory>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueProviderFactoryCollection"/> class that is empty.
        /// </summary>
        public ValueProviderFactoryCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueProviderFactoryCollection"/> class
        /// as a wrapper for the specified list.
        /// </summary>
        /// <param name="valueProviderFactories">The list that is wrapped by the new collection.</param>
        public ValueProviderFactoryCollection(IList<IValueProviderFactory> valueProviderFactories)
            : base(valueProviderFactories)
        {
        }

        /// <summary>
        /// Removes all value provider factories of the specified type.
        /// </summary>
        /// <typeparam name="TValueProviderFactory">The type to remove.</typeparam>
        public void RemoveType<TValueProviderFactory>() where TValueProviderFactory : IValueProviderFactory
        {
            RemoveType(typeof(TValueProviderFactory));
        }

        /// <summary>
        /// Removes all value provider factories of the specified type.
        /// </summary>
        /// <param name="valueProviderFactoryType">The type to remove.</param>
        public void RemoveType(Type valueProviderFactoryType)
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var valueProviderFactory = this[i];
                if (valueProviderFactory.GetType() == valueProviderFactoryType)
                {
                    RemoveAt(i);
                }
            }
        }
    }
}
