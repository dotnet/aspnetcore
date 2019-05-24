// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A collection of <see cref="ModelError"/> instances.
    /// </summary>
    public class ModelErrorCollection : Collection<ModelError>
    {
        /// <summary>
        /// Adds the specified <paramref name="exception"/> instance.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/></param>
        public void Add(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Add(new ModelError(exception));
        }

        /// <summary>
        /// Adds the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        public void Add(string errorMessage)
        {
            if (errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }

            Add(new ModelError(errorMessage));
        }
    }
}
