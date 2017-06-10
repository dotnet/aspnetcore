// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class ModelError
    {
        public ModelError(Exception exception)
            : this(exception, errorMessage: null)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }
        }

        public ModelError(Exception exception, string errorMessage)
            : this(errorMessage)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Exception = exception;
        }

        public ModelError(string errorMessage)
        {
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public Exception Exception { get; }

        public string ErrorMessage { get; }
    }
}
