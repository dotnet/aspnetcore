// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// this controls if the logs are written to the console.
// they can be reviewed for general content.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    public class LogEntry
    {
        public LogEntry() { }

        public int EventId { get; set; }

        public Exception Exception { get; set; }

        public Func<object, Exception, string> Formatter { get; set; }

        public LogLevel Level { get; set; }

        public object State { get; set; }

        public override string ToString()
        {
            if (Formatter != null)
            {
                return Formatter(this.State, this.Exception);
            }
            else
            {
                string message = (Formatter != null ? Formatter(State, Exception) : (State?.ToString() ?? "null"));
                message += ", LogLevel: " + Level.ToString();
                message += ", EventId: " + EventId.ToString();
                message += ", Exception: " + (Exception == null ? "null" : Exception.Message);
                return message;
            }
        }
    }    
}
