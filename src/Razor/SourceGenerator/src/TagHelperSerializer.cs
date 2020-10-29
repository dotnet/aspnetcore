// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;

namespace RazorSourceGenerators
{
    internal class TagHelperSerializer
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            Converters =
            {
                new TagHelperDescriptorJsonConverter(),
                new RazorDiagnosticJsonConverter(),
            }
        };

        public static void Serialize(string manifestFilePath, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            using var stream = File.OpenWrite(manifestFilePath);
            using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096, leaveOpen: true);

            Serializer.Serialize(writer, tagHelpers);
        }

        public static IReadOnlyList<TagHelperDescriptor> Deserialize(string manifestFilePath)
        {
            using var stream = File.OpenRead(manifestFilePath);
            using var reader = new JsonTextReader(new StreamReader(stream));

            return Serializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
        }
    }
}
