// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR.Client
{
    [Serializable]
    public class HubException : Exception
    {
        public HubException()
        {
        }

        public HubException(string message) : base(message)
        {
        }

        public HubException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
