// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Web
{
    internal class InvalidWebEventInputException : Exception
    {
        public InvalidWebEventInputException(string message) : base(message) { }

        public InvalidWebEventInputException(string message, Exception inner) : base(message, inner) { }     
    }
}
