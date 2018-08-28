// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.HttpRepl.OpenApi
{
    public class SwaggerV2EndpointMetadataReader : IEndpointMetadataReader
    {
        public bool CanHandle(JObject document)
        {
            return (document["swagger"]?.ToString() ?? "").StartsWith("2.", StringComparison.Ordinal);
        }

        public IEnumerable<EndpointMetadata> ReadMetadata(JObject document)
        {
            List<EndpointMetadata> metadata = new List<EndpointMetadata>();

            if (!(document["consumes"] is JArray globalConsumes))
            {
                globalConsumes = new JArray();
            }

            if (document["paths"] is JObject obj)
            {
                foreach (JProperty property in obj.Properties())
                {
                    if (!(property.Value is JObject requestMethodInfos))
                    {
                        continue;
                    }

                    Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<Parameter>>> requestMethods = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<Parameter>>>(StringComparer.Ordinal);

                    foreach (JProperty methodInfo in requestMethodInfos.Properties())
                    {
                        List<Parameter> parameters = new List<Parameter>();

                        if (methodInfo.Value is JObject methodInfoDescription)
                        {
                            if (methodInfoDescription["parameters"] is JArray parametersArray)
                            {
                                foreach (JObject parameterObj in parametersArray.OfType<JObject>())
                                {
                                    //TODO: Resolve refs here

                                    Parameter p = parameterObj.ToObject<Parameter>();
                                    p.Location = parameterObj["in"]?.ToString();
                                    p.IsRequired = parameterObj["required"]?.ToObject<bool>() ?? false;

                                    if (!(parameterObj["schema"] is JObject schemaObject))
                                    {
                                        schemaObject = null;
                                    }

                                    p.Schema = schemaObject?.ToObject<Schema>() ?? parameterObj.ToObject<Schema>();
                                    parameters.Add(p);
                                }
                            }

                            if (!(methodInfoDescription["consumes"] is JArray consumes))
                            {
                                consumes = globalConsumes;
                            }

                            Dictionary<string, IReadOnlyList<Parameter>> parametersByContentType = new Dictionary<string, IReadOnlyList<Parameter>>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "", parameters }
                            };

                            foreach (JValue value in consumes.OfType<JValue>().Where(x => x.Type == JTokenType.String))
                            {
                                parametersByContentType[value.ToString()] = parameters;
                            }

                            requestMethods[methodInfo.Name] = parametersByContentType;
                        }
                    }

                    metadata.Add(new EndpointMetadata(property.Name, requestMethods));
                }
            }

            return metadata;
        }
    }
}
