// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// Exception thrown by <see cref="IInputFormatter"/> when the input is not in an expected format.
    /// </summary>
    public class InputFormatterException : Exception
    {
        public InputFormatterException()
        {
        }

        public InputFormatterException(string message)
            : base(message)
        {
        }

        public InputFormatterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
