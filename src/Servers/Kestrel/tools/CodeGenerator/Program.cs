// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CodeGenerator;

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

        Run(args[0], args[1], args[2], args[3], args[4]);

        return 0;
    }

    public static void Run(
        string knownHeadersPath,
        string httpProtocolFeatureCollectionPath,
        string httpUtilitiesPath,
        string transportMultiplexedConnectionFeatureCollectionPath,
        string transportConnectionFeatureCollectionPath)
    {
        var knownHeadersContent = KnownHeaders.GeneratedFile();
        var httpProtocolFeatureCollectionContent = HttpProtocolFeatureCollection.GenerateFile();
        var httpUtilitiesContent = HttpUtilities.HttpUtilities.GeneratedFile();
        var transportMultiplexedConnectionFeatureCollectionContent = TransportMultiplexedConnectionFeatureCollection.GenerateFile();
        var transportConnectionFeatureCollectionContent = TransportConnectionFeatureCollection.GenerateFile();

        UpdateFile(knownHeadersPath, knownHeadersContent);
        UpdateFile(httpProtocolFeatureCollectionPath, httpProtocolFeatureCollectionContent);
        UpdateFile(httpUtilitiesPath, httpUtilitiesContent);
        UpdateFile(transportMultiplexedConnectionFeatureCollectionPath, transportMultiplexedConnectionFeatureCollectionContent);
        UpdateFile(transportConnectionFeatureCollectionPath, transportConnectionFeatureCollectionContent);
    }

    public static void UpdateFile(string path, string content)
    {
        var existingContent = File.Exists(path) ? File.ReadAllText(path) : "";
        if (!string.Equals(content, existingContent))
        {
            File.WriteAllText(path, content);
            Console.WriteLine($"{path} updated.");
        }
        else
        {
            Console.WriteLine($"{path} already up to date.");
        }
    }
}
