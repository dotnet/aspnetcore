// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

[assembly: ProducesErrorResponseType(typeof(Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.SymbolApiResponseMetadataProviderTest.GetErrorResponseType_ReturnsTypeDefinedAtAssemblyModel))]

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.SymbolApiResponseMetadataProviderTest
{
    public class GetErrorResponseType_ReturnsTypeDefinedAtAssemblyController
    {
        public void Action() { }
    }

    public class GetErrorResponseType_ReturnsTypeDefinedAtAssemblyModel { }
}
