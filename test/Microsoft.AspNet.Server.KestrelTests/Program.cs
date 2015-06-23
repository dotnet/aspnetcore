// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Server.KestrelTests
{
    /// <summary>
    /// Summary description for Program
    /// </summary>
    public class Program
    {
        public void Main()
        {
            new EngineTests().DisconnectingClient().Wait();
        }
    }
}