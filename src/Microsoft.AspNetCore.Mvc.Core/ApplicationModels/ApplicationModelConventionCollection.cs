// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Represents a collection of application model conventions.
    /// </summary>
    public class ApplicationModelConventionCollection : Collection<IApplicationModelConvention>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationModelConventionCollection"/> class that is empty.
        /// </summary>
        public ApplicationModelConventionCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationModelConventionCollection"/> class
        /// as a wrapper for the specified list.
        /// </summary>
        /// <param name="applicationModelConventions">The list that is wrapped by the new collection.</param>
        public ApplicationModelConventionCollection(IList<IApplicationModelConvention> applicationModelConventions)
            : base(applicationModelConventions)
        {
        }

        /// <summary>
        /// Removes all application model conventions of the specified type.
        /// </summary>
        /// <typeparam name="TApplicationModelConvention">The type to remove.</typeparam>
        public void RemoveType<TApplicationModelConvention>() where TApplicationModelConvention : IApplicationModelConvention
        {
            RemoveType(typeof(TApplicationModelConvention));
        }

        /// <summary>
        /// Removes all application model conventions of the specified type.
        /// </summary>
        /// <param name="applicationModelConventionType">The type to remove.</param>
        public void RemoveType(Type applicationModelConventionType)
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var applicationModelConvention = this[i];
                if (applicationModelConvention.GetType() == applicationModelConventionType)
                {
                    RemoveAt(i);
                }
            }
        }
    }
}
