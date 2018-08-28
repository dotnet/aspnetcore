// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.HttpRepl.OpenApi
{
    public class EndpointMetadataReader
    {
        private readonly List<IEndpointMetadataReader> _readers = new List<IEndpointMetadataReader>
        {
            new OpenApiV3EndpointMetadataReader(),
            new SwaggerV2EndpointMetadataReader(),
            new SwaggerV1EndpointMetadataReader()
        };

        public void RegisterReader(IEndpointMetadataReader reader)
        {
            _readers.Add(reader);
        }

        public IEnumerable<EndpointMetadata> Read(JObject document)
        {
            foreach (IEndpointMetadataReader reader in _readers)
            {
                if (reader.CanHandle(document))
                {
                    IEnumerable<EndpointMetadata> result = reader.ReadMetadata(document);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
