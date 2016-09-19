// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    /// <summary>
    /// An exception whose stack trace should be suppressed in console output
    /// </summary>
    public class GracefulException : Exception
    {
        public GracefulException()
        {
        }

        public GracefulException(string message) : base(message)
        {
        }

        public GracefulException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
