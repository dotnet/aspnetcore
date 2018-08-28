// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.HttpRepl.OpenApi
{
    public class OpenApiV3EndpointMetadataReader : IEndpointMetadataReader
    {
        public bool CanHandle(JObject document)
        {
            return (document["openapi"]?.ToString() ?? "").StartsWith("3.", StringComparison.Ordinal);
        }

        public IEnumerable<EndpointMetadata> ReadMetadata(JObject document)
        {
            List<EndpointMetadata> metadata = new List<EndpointMetadata>();

            if (document["paths"] is JObject paths)
            {
                foreach (JProperty path in paths.Properties())
                {
                    if (!(path.Value is JObject pathBody))
                    {
                        continue;
                    }

                    Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<Parameter>>> requestMethods = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<Parameter>>>(StringComparer.OrdinalIgnoreCase);

                    foreach (JProperty method in pathBody.Properties())
                    {
                        List<Parameter> parameters = new List<Parameter>();

                        if (method.Value is JObject methodBody)
                        {
                            if (methodBody["parameters"] is JArray parametersArray)
                            {
                                foreach (JObject parameterObj in parametersArray.OfType<JObject>())
                                {
                                    Parameter p = parameterObj.ToObject<Parameter>();
                                    p.Location = parameterObj["in"]?.ToString();

                                    if (!(parameterObj["schema"] is JObject schemaObject))
                                    {
                                        schemaObject = null;
                                    }

                                    p.Schema = schemaObject?.ToObject<Schema>() ?? parameterObj.ToObject<Schema>();
                                    parameters.Add(p);
                                }
                            }

                            if (methodBody["requestBody"] is JObject bodyObject)
                            {
                                if (!(bodyObject["content"] is JObject contentTypeLookup))
                                {
                                    continue;
                                }

                                foreach (JProperty contentTypeEntry in contentTypeLookup.Properties())
                                {
                                    List<Parameter> parametersByContentType = new List<Parameter>(parameters);
                                    Parameter p = bodyObject.ToObject<Parameter>();
                                    p.Location = "body";
                                    p.IsRequired = bodyObject["required"]?.ToObject<bool>() ?? false;

                                    if (!(bodyObject["schema"] is JObject schemaObject))
                                    {
                                        schemaObject = null;
                                    }

                                    p.Schema = schemaObject?.ToObject<Schema>() ?? bodyObject.ToObject<Schema>();
                                    parametersByContentType.Add(p);

                                    Dictionary<string, IReadOnlyList<Parameter>> bucketByMethod;
                                    if (!requestMethods.TryGetValue(method.Name, out IReadOnlyDictionary<string, IReadOnlyList<Parameter>> bucketByMethodRaw))
                                    {
                                        requestMethods[method.Name] = bucketByMethodRaw = new Dictionary<string, IReadOnlyList<Parameter>>(StringComparer.OrdinalIgnoreCase)
                                        {
                                            { "", parametersByContentType }
                                        };
                                    }

                                    bucketByMethod = (Dictionary<string, IReadOnlyList<Parameter>>)bucketByMethodRaw;
                                    bucketByMethod[contentTypeEntry.Name] = parametersByContentType;
                                }
                            }
                        }
                    }

                    metadata.Add(new EndpointMetadata(path.Name, requestMethods));
                }
            }

            return metadata;
        }
    }
}
