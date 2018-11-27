// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis;

namespace ControllersFromServicesWebSite
{
    public class AssemblyMetadataReferenceFeatureProvider : IApplicationFeatureProvider<MetadataReferenceFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, MetadataReferenceFeature feature)
        {
            var currentAssembly = GetType().GetTypeInfo().Assembly;
            feature.MetadataReferences.Add(MetadataReference.CreateFromFile(currentAssembly.Location));
        }
    }
}
