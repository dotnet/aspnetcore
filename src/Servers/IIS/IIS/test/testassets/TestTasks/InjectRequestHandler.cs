// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestTasks;

public class InjectRequestHandler
{
    private const string aspnetcoreV2Name = "aspnetcorev2_inprocess.dll";

    private static void Main(string[] args)
    {
        string depsFile = args[0];
        string rid = "";
        if (args.Length > 1)
        {
            rid = args[1];
        }

        JToken deps;
        using (var file = File.OpenText(depsFile))
        using (JsonTextReader reader = new JsonTextReader(file))
        {
            deps = JObject.ReadFrom(reader);
        }

        var libraryName = "ANCMRH/1.0";
        var libraries = (JObject)deps["libraries"];
        var targetName = (JValue)deps["runtimeTarget"]["name"];

        var target = (JObject)deps["targets"][targetName.Value];
        var targetLibrary = target.Properties().FirstOrDefault(p => p.Name == libraryName);
        targetLibrary?.Remove();

        var bitness = new JObject();
        if (string.IsNullOrEmpty(rid))
        {
            bitness.Add(new JProperty($"x64/{aspnetcoreV2Name}", new JObject(
                new JProperty("rid", "win-x64"),
                new JProperty("assetType", "native")
            )));
            bitness.Add(new JProperty($"x86/{aspnetcoreV2Name}", new JObject(
                new JProperty("rid", "win-x86"),
                new JProperty("assetType", "native")
            )));
        }
        else
        {
            bitness.Add(new JProperty(aspnetcoreV2Name, new JObject(
                new JProperty("rid", rid),
                new JProperty("assetType", "native")
            )));
            var outputFolder = Path.GetDirectoryName(depsFile);
            var bitnessString = rid.Substring(rid.Length - 3, 3);
            File.Copy(Path.Combine(outputFolder, bitnessString, aspnetcoreV2Name), Path.Combine(outputFolder, aspnetcoreV2Name), overwrite: true);
        }

        targetLibrary =
            new JProperty(libraryName, new JObject(
                new JProperty("runtimeTargets", bitness)));

        target.AddFirst(targetLibrary);

        var library = libraries.Properties().FirstOrDefault(p => p.Name == libraryName);
        library?.Remove();
        library =
             new JProperty(libraryName, new JObject(
                 new JProperty("type", "package"),
                 new JProperty("serviceable", true),
                 new JProperty("sha512", ""),
                 new JProperty("path", libraryName),
                 new JProperty("hashPath", "")));
        libraries.AddFirst(library);

        using (var file = File.CreateText(depsFile))
        using (var writer = new JsonTextWriter(file) { Formatting = Formatting.Indented })
        {
            deps.WriteTo(writer);
        }
    }
}
