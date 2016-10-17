// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.WebSockets.Internal.ConformanceTest.Autobahn
{
    /// <summary>
    /// Wrapper around the Autobahn Test Suite's "wstest" app.
    /// </summary>
    public class Wstest : Executable
    {
        private static Lazy<Wstest> _instance = new Lazy<Wstest>(Create);

        public static Wstest Default => _instance.Value;

        public Wstest(string path) : base(path) { }

        private static Wstest Create()
        {
            var location = Locate("wstest");
            return location == null ? null : new Wstest(location);
        }
    }
}
