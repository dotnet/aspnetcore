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

            Run(args[0], args[1], args[2]);

            return 0;
        }

        public static void Run(string knownHeadersPath, string httpProtocolFeatureCollectionPath, string httpUtilitiesPath)
        {
            var knownHeadersContent = KnownHeaders.GeneratedFile();
            var httpProtocolFeatureCollectionContent = HttpProtocolFeatureCollection.GeneratedFile("HttpProtocol");
            var httpUtilitiesContent = HttpUtilities.HttpUtilities.GeneratedFile();

            var existingKnownHeaders = File.Exists(knownHeadersPath) ? File.ReadAllText(knownHeadersPath) : "";
            if (!string.Equals(knownHeadersContent, existingKnownHeaders))
            {
                File.WriteAllText(knownHeadersPath, knownHeadersContent);
            }

            var existingHttpProtocolFeatureCollection = File.Exists(httpProtocolFeatureCollectionPath) ? File.ReadAllText(httpProtocolFeatureCollectionPath) : "";
            if (!string.Equals(httpProtocolFeatureCollectionContent, existingHttpProtocolFeatureCollection))
            {
                File.WriteAllText(httpProtocolFeatureCollectionPath, httpProtocolFeatureCollectionContent);
            }

            var existingHttpUtilities = File.Exists(httpUtilitiesPath) ? File.ReadAllText(httpUtilitiesPath) : "";
            if (!string.Equals(httpUtilitiesContent, existingHttpUtilities))
            {
                File.WriteAllText(httpUtilitiesPath, httpUtilitiesContent);
            }
        }
    }
}
