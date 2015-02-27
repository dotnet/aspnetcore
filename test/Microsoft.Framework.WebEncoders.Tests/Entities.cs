// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.Framework.WebEncoders
{
    internal static class Entities
    {
        public static readonly IDictionary<string, ParsedEntity> ParsedEntities = GetParsedEntities();

        private static IDictionary<string, ParsedEntity> GetParsedEntities()
        {
            // read all entries
            string allEntitiesText = ReadEntitiesJsonFile();
            var deserializedRawData = new JsonSerializer().Deserialize<IDictionary<string, ParsedEntity>>(new JsonTextReader(new StringReader(allEntitiesText)));

            // strip out all entries which aren't of the form "&entity;"
            foreach (var key in deserializedRawData.Keys.ToArray() /* dupe since we're mutating original structure */)
            {
                if (!key.StartsWith("&", StringComparison.Ordinal) || !key.EndsWith(";", StringComparison.Ordinal))
                {
                    deserializedRawData.Remove(key);
                }
            }
            return deserializedRawData;
        }

        private static string ReadEntitiesJsonFile()
        {
            return File.ReadAllText("entities.json");
        }
    }
}
