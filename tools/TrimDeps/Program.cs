// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Program
{
    public static void Main(string[] args)
    {
        ChangeEntryPointLibraryName(args[0]);
    }

    private static void ChangeEntryPointLibraryName(string depsFile)
    {
        JToken deps;
        using (var file = File.OpenText(depsFile))
        using (JsonTextReader reader = new JsonTextReader(file))
        {
            deps = JObject.ReadFrom(reader);
        }

        string version = null;
        foreach (JProperty target in deps["targets"])
        {
            var targetLibrary = target.Value.Children<JProperty>().FirstOrDefault();
            if (targetLibrary == null)
            {
                continue;
            }

            targetLibrary.Remove();
        }
        if (version != null)
        {
            var library = deps["libraries"].Children<JProperty>().First();
            library.Remove();

            using (var file = File.CreateText(depsFile))
            using (var writer = new JsonTextWriter(file) { Formatting = Formatting.Indented })
            {
                deps.WriteTo(writer);
            }
        }
    }
}