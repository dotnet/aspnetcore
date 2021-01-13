// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Interface for model binding collections.
    /// </summary>
    public interface ICollectionModelBinder : IModelBinder
    {
        /// <summary>
        /// Gets an indication whether or not this <see cref="ICollectionModelBinder"/> implementation can create
        /// an <see cref="object"/> assignable to <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType"><see cref="Type"/> of the model.</param>
        /// <returns>
        /// <c>true</c> if this <see cref="ICollectionModelBinder"/> implementation can create an <see cref="object"/>
        /// assignable to <paramref name="targetType"/>; <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        /// A <c>true</c> return value is necessary for successful model binding if model is initially <c>null</c>.
        /// </remarks>
        bool CanCreateInstance(Type targetType);
    }
}
