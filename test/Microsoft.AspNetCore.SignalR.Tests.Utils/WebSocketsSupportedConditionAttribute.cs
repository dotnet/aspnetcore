// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class WebSocketsSupportedConditionAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
#if NETCOREAPP2_1
                // .NET Core 2.1 and greater has sockets
                return true;
#else
                // Non-Windows platforms have sockets
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return true;
                }

                // Windows 8 and greater has sockets
                if (Environment.Version >= new Version(6, 2))
                {
                    return true;
                }

                return false;
#endif
            }
        }

        public string SkipReason => "No WebSockets Client for this platform";
    }
}
