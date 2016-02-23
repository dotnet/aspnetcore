// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Server.Kestrel.Exceptions
{
    public sealed class BadHttpRequestException : IOException
    {
        internal BadHttpRequestException(string message)
            : base(message)
        {

        }
    }
}
