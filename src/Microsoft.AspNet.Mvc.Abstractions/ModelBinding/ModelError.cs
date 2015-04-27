// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelError
    {
        public ModelError([NotNull]Exception exception)
            : this(exception, errorMessage: null)
        {
        }

        public ModelError([NotNull]Exception exception, string errorMessage)
            : this(errorMessage)
        {
            Exception = exception;
        }

        public ModelError(string errorMessage)
        {
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public Exception Exception { get; private set; }

        public string ErrorMessage { get; private set; }
    }
}
