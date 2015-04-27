// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class LineMappingManager
    {
        public LineMappingManager()
        {
            Mappings = new List<LineMapping>();
        }

        public List<LineMapping> Mappings { get; }

        public void AddMapping(MappingLocation documentLocation, MappingLocation generatedLocation)
        {
            Mappings.Add(new LineMapping(documentLocation, generatedLocation));
        }
    }
}
