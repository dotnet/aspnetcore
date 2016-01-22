// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class ModelErrorCollection : Collection<ModelError>
    {
        public void Add(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Add(new ModelError(exception));
        }

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
