// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    // See the details of the component serialization protocol in WebAssemblyComponentDeserializer.cs on the Components solution.
    internal class WebAssemblyComponentSerializer
    {
        public WebAssemblyComponentMarker SerializeInvocation(Type type, ParameterView parameters, bool prerendered)
        {
            var assembly = type.Assembly.GetName().Name;
            var typeFullName = type.FullName;
            var (definitions, values) = ComponentParameter.FromParameterView(parameters);

            // We need to serialize and Base64 encode parameters separately since they can contain arbitrary data that might
            // cause the HTML comment to be invalid (like if you serialize a string that contains two consecutive dashes "--").
            var serializedDefinitions = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(definitions, WebAssemblyComponentSerializationSettings.JsonSerializationOptions));
            var serializedValues = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(values, WebAssemblyComponentSerializationSettings.JsonSerializationOptions));

            return prerendered ? WebAssemblyComponentMarker.Prerendered(assembly, typeFullName, serializedDefinitions, serializedValues) :
                WebAssemblyComponentMarker.NonPrerendered(assembly, typeFullName, serializedDefinitions, serializedValues);
        }

        internal IEnumerable<string> GetPreamble(WebAssemblyComponentMarker record)
        {
            var serializedStartRecord = JsonSerializer.Serialize(
                record,
                WebAssemblyComponentSerializationSettings.JsonSerializationOptions);

            if (record.PrerenderId != null)
            {
                return PrerenderedStart(serializedStartRecord);
            }
            else
            {
                return NonPrerenderedSequence(serializedStartRecord);
            }

            static IEnumerable<string> PrerenderedStart(string startRecord)
            {
                yield return "<!--Blazor:";
                yield return startRecord;
                yield return "-->";
            }

            static IEnumerable<string> NonPrerenderedSequence(string record)
            {
                yield return "<!--Blazor:";
                yield return record;
                yield return "-->";
            }
        }

        internal IEnumerable<string> GetEpilogue(WebAssemblyComponentMarker record)
        {
            var serializedStartRecord = JsonSerializer.Serialize(
                record.GetEndRecord(),
                WebAssemblyComponentSerializationSettings.JsonSerializationOptions);

            return PrerenderEnd(serializedStartRecord);

            static IEnumerable<string> PrerenderEnd(string endRecord)
            {
                yield return "<!--Blazor:";
                yield return endRecord;
                yield return "-->";
            }
        }
    }
}
