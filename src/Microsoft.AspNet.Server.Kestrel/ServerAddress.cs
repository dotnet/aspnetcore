// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class ServerAddress
    {
        public string Host { get; private set; }
        public string Path { get; private set; }
        public int Port { get; private set; }
        public string Scheme { get; private set; }

        public static ServerAddress FromUrl(string url)
        {
            url = url ?? string.Empty;

            int delimiterStart1 = url.IndexOf("://", StringComparison.Ordinal);
            if (delimiterStart1 < 0)
            {
                return null;
            }
            int delimiterEnd1 = delimiterStart1 + "://".Length;

            int delimiterStart3 = url.IndexOf("/", delimiterEnd1, StringComparison.Ordinal);
            if (delimiterStart3 < 0)
            {
                delimiterStart3 = url.Length;
            }
            int delimiterStart2 = url.LastIndexOf(":", delimiterStart3 - 1, delimiterStart3 - delimiterEnd1, StringComparison.Ordinal);
            int delimiterEnd2 = delimiterStart2 + ":".Length;
            if (delimiterStart2 < 0)
            {
                delimiterStart2 = delimiterStart3;
                delimiterEnd2 = delimiterStart3;
            }
            var serverAddress = new ServerAddress();
            serverAddress.Scheme = url.Substring(0, delimiterStart1);
            string portString = url.Substring(delimiterEnd2, delimiterStart3 - delimiterEnd2);
            int portNumber;
            if (int.TryParse(portString, NumberStyles.Integer, CultureInfo.InvariantCulture, out portNumber))
            {
                serverAddress.Host = url.Substring(delimiterEnd1, delimiterStart2 - delimiterEnd1);
                serverAddress.Port = portNumber;
            }
            else
            {
                if (string.Equals(serverAddress.Scheme, "http", StringComparison.OrdinalIgnoreCase))
                {
                    serverAddress.Port = 80;
                }
                else if (string.Equals(serverAddress.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                {
                    serverAddress.Port = 443;
                }
                else
                {
                    serverAddress.Port = 0;
                }
                serverAddress.Host = url.Substring(delimiterEnd1, delimiterStart3 - delimiterEnd1);
            }
            serverAddress.Path = url.Substring(delimiterStart3);
            return serverAddress;
        }
    }
}