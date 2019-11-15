// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace CodeGenerator
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Missing path to HttpHeaders.Generated.cs");
                return 1;
            }
            else if (args.Length < 2)
            {
                Console.Error.WriteLine("Missing path to HttpProtocol.Generated.cs");
                return 1;
            }
            else if (args.Length < 3)
            {
                Console.Error.WriteLine("Missing path to HttpUtilities.Generated.cs");
                return 1;
            }
            else if (args.Length < 4)
            {
                Console.Error.WriteLine("Missing path to TransportMultiplexedConnection.Generated.cs");
                return 1;
            }
            else if (args.Length < 5)
            {
                Console.Error.WriteLine("Missing path to TransportConnection.Generated.cs");
                return 1;
            }
            else if (args.Length < 6)
            {
                Console.Error.WriteLine("Missing path to Http2Connection.Generated.cs");
                return 1;
            }
            else if (args.Length < 7)
            {
                Console.Error.WriteLine("Missing path to TransportStream.Generated.cs");
                return 1;
            }

            Run(args[0], args[1], args[2], args[3], args[4], args[5], args[6]);

            return 0;
        }

        public static void Run(
            string knownHeadersPath,
            string httpProtocolFeatureCollectionPath,
            string httpUtilitiesPath,
            string http2ConnectionPath,
            string transportMultiplexedConnectionFeatureCollectionPath,
            string transportConnectionFeatureCollectionPath,
            string transportStreamFeatureCollectionPath)
        {
            var knownHeadersContent = KnownHeaders.GeneratedFile();
            var httpProtocolFeatureCollectionContent = HttpProtocolFeatureCollection.GenerateFile();
            var httpUtilitiesContent = HttpUtilities.HttpUtilities.GeneratedFile();
            var transportMultiplexedConnectionFeatureCollectionContent = TransportMultiplexedConnectionFeatureCollection.GenerateFile();
            var transportConnectionFeatureCollectionContent = TransportConnectionFeatureCollection.GenerateFile();
            var http2ConnectionContent = Http2Connection.GenerateFile();
            var transportStreamFeatureCollectionContent = TransportStreamFeatureCollection.GenerateFile();

            UpdateFile(knownHeadersPath, knownHeadersContent);
            UpdateFile(httpProtocolFeatureCollectionPath, httpProtocolFeatureCollectionContent);
            UpdateFile(httpUtilitiesPath, httpUtilitiesContent);
            UpdateFile(transportMultiplexedConnectionFeatureCollectionPath, transportMultiplexedConnectionFeatureCollectionContent);
            UpdateFile(transportConnectionFeatureCollectionPath, transportConnectionFeatureCollectionContent);
            UpdateFile(transportStreamFeatureCollectionPath, transportStreamFeatureCollectionContent);
        }

        public static void UpdateFile(string path, string content)
        {
            var existingContent = File.Exists(path) ? File.ReadAllText(path) : "";
            if (!string.Equals(content, existingContent))
            {
                File.WriteAllText(path, content);
            }

            var existingHttp2Connection = File.Exists(http2ConnectionPath) ? File.ReadAllText(http2ConnectionPath) : "";
            if (!string.Equals(http2ConnectionContent, existingHttp2Connection))
            {
                File.WriteAllText(http2ConnectionPath, http2ConnectionContent);
            }
        }
    }
}
