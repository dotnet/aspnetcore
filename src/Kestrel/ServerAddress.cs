// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Kestrel
{
    public class ServerAddress
    {
        public string Host { get; internal set; }
        public string Path { get; internal set; }
        public int Port { get; internal set; }
        public string Scheme { get; internal set; }
    }
}